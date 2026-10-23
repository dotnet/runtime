// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/gcref.h"
#include "../../../../src/coreclr/gc/lxr_reference_counting.h"
#include "../../../../src/coreclr/gc/lxr_finalizer_pause.h"
#include "../../../../src/coreclr/gc/lxr_collection_policy.h"
#include "../../../../src/coreclr/gc/lxr_worker_policy.h"
#include "../../../../src/coreclr/gc/lxr_work_scheduler.h"

#include <atomic>
#include <algorithm>
#include <array>
#include <chrono>
#include <cstdio>
#include <memory>
#include <random>
#include <set>
#include <thread>
#include <vector>
#if defined(__linux__)
#include <sys/mman.h>
#include <unistd.h>
#endif

bool g_oldMethodTableFlags = false;
void LxrTestFailCommitAfter(int64_t calls);

namespace
{
    constexpr uint32_t Timeout = 30'000;
    constexpr size_t TraceBatchFixtureMaximumWorkBytes = 512 * 1024;
    constexpr size_t TraceBatchFixtureMaximumScanBytes = 4 * 1024;
    int Checks = 0;
    int Failures = 0;

    // Evidence for the epoch gate: both counts must stay zero in every selector.
    struct FlipReport final
    {
        ~FlipReport()
        {
            std::printf("flip_overlaps=%llu flip_blocked=%llu\n",
                static_cast<unsigned long long>(ImmixBlockManager::GetFlipOverlaps()),
                static_cast<unsigned long long>(ImmixBlockManager::GetFlipBlockedOperations()));
        }
    } FlipReportAtExit;
    bool Check(bool condition, const char* message)
    {
        Checks++;
        if (!condition)
        {
            Failures++;
            std::fprintf(stderr, "FAIL: %s\n", message);
        }
        return condition;
    }

    class Type final
    {
    public:
        Type(uint32_t size, size_t fields, size_t fieldOffset = sizeof(void*))
            : m_bytes((fields == 0 ? 0 : CGCDesc::ComputeSize(1)) + sizeof(MethodTable), 0),
              table(reinterpret_cast<MethodTable*>(m_bytes.data() +
                  (fields == 0 ? 0 : CGCDesc::ComputeSize(1))))
        {
            table->m_baseSize = size;
            table->m_flags = fields == 0 ? 0 : MTFlag_ContainsGCPointers;
            if (fields != 0)
            {
                CGCDesc::Init(table, 1);
                CGCDescSeries* series = CGCDesc::GetCGCDescFromMT(table)->GetLowestSeries();
                series->SetSeriesOffset(fieldOffset);
                series->SetSeriesSize(fields * sizeof(void*) - size);
            }
        }

    private:
        std::vector<uint8_t> m_bytes;

    public:
        MethodTable* table;
    };

    void Suspend(void*) {}
    void Resume(bool, void*) {}

    struct Roots
    {
        const std::vector<Object*>* objects;
    };

    LxrRcResult Capture(LxrReferenceCounting* engine, void* context)
    {
        Roots* roots = static_cast<Roots*>(context);
        for (Object* value : *roots->objects)
        {
            Object* callbackScopedTarget = value;
            LxrRcResult result = engine->AddRoot(callbackScopedTarget);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        return LxrRcResult::Success;
    }

    bool Epoch(LxrReferenceCounting& engine, const std::vector<Object*>& objects)
    {
        Roots roots{&objects};
        return Check(engine.Collect(Capture, &roots, Timeout) == LxrRcResult::Success,
                "RC epoch completes") &&
            Check(engine.Drain(Timeout) == LxrRcResult::Success, "RC lazy tail drains");
    }

    bool Initialize(LxrReferenceCounting& engine, uint8_t bits, LxrRcMutator** mutator, void* key)
    {
        LxrRcConfiguration configuration{
            16 * ImmixBlockGeometry::BlockBytes,
            8 * LxrLargeObjectSpace::MinimumObjectBytes,
            16, 1024, 4, bits,
        };
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        return Check(engine.Initialize(configuration, adapter) == LxrRcResult::Success,
                "shared product engine initializes") &&
            Check(engine.AttachMutator(key, mutator) == LxrRcResult::Success,
                "native mutator attaches");
    }

    Object* Allocate(LxrReferenceCounting& engine, LxrRcMutator* mutator, Type& type);
    Object** Field(Object* object, size_t index = 0);
    bool Store(LxrReferenceCounting& engine, LxrRcMutator* mutator, Object** slot, Object* target);

    struct MovingRoots
    {
        std::vector<Object*> objects;
        Object* pinned = nullptr;
        Object* pinCandidate = nullptr;
        bool pinFirst = false;
        Object* interior = nullptr;
        // S7. Pins that the capture adds besides `pinned`; the interior root as a pin; several threads that add
        // every object of pinThreadObjects twice (two different shards).
        std::vector<Object*> extraPins;
        bool pinInterior = false;
        size_t pinThreads = 0;
        std::vector<Object*> pinThreadObjects;
        // S7. What the pause knows while it runs (the relocation callback runs inside the pause).
        bool probe = false;
        Object* probeNeighbour = nullptr;
        size_t probeBlocks = SIZE_MAX;
        bool probePinned = false;
        bool probeNeighbourPinned = true;
    };

    LxrRcResult CaptureMoving(LxrReferenceCounting* engine, void* context)
    {
        MovingRoots* roots = static_cast<MovingRoots*>(context);
        if (roots->pinCandidate != nullptr && roots->pinFirst)
        {
            roots->pinned = roots->pinCandidate;
            LxrRcResult result = engine->AddRoot(roots->pinned);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        for (Object* object : roots->objects)
        {
            LxrRcResult result = engine->AddRoot(object);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        if (roots->pinCandidate != nullptr && !roots->pinFirst)
        {
            roots->pinned = roots->pinCandidate;
            LxrRcResult result = engine->AddRoot(roots->pinned);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        // S7: the pin list replaces the pin callback. A pause pins the object that the test names, in every capture.
        if (roots->pinned != nullptr)
        {
            LxrRcResult result = engine->AddPinnedRoot(roots->pinned);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        for (Object* extra : roots->extraPins)
        {
            LxrRcResult result = engine->AddPinnedRoot(extra);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        if (roots->pinThreads != 0)
        {
            std::atomic<int> failures{0};
            std::vector<std::thread> threads;
            for (size_t thread = 0; thread < roots->pinThreads; thread++)
            {
                threads.emplace_back([&, thread]()
                {
                    for (size_t index = 0; index < roots->pinThreadObjects.size(); index++)
                    {
                        // Each object is added by two threads, so the merge has to remove duplicates.
                        if ((index + thread) % roots->pinThreads < 2)
                        {
                            if (engine->AddPinnedRoot(roots->pinThreadObjects[index], false, thread) !=
                                LxrRcResult::Success)
                            {
                                failures++;
                            }
                        }
                    }
                });
            }
            for (std::thread& thread : threads)
            {
                thread.join();
            }
            if (failures.load() != 0)
            {
                return LxrRcResult::InvariantViolation;
            }
        }
        if (roots->interior != nullptr && roots->pinInterior)
        {
            LxrRcResult result = engine->AddPinnedRoot(roots->interior, true);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        return roots->interior == nullptr ? LxrRcResult::Success : engine->AddRoot(roots->interior, true);
    }

    LxrRcResult RelocateMoving(LxrReferenceCounting* engine, void* context)
    {
        MovingRoots* roots = static_cast<MovingRoots*>(context);
        if (roots->probe)
        {
            roots->probeBlocks = engine->CountPausePinnedBlocks();
            roots->probePinned = roots->pinned != nullptr && engine->IsPinnedForPause(roots->pinned);
            roots->probeNeighbourPinned = roots->probeNeighbour != nullptr &&
                engine->IsPinnedForPause(roots->probeNeighbour);
        }
        for (Object*& object : roots->objects)
        {
            LxrRcResult result = engine->RelocateRoot(&object, false, object == roots->pinned);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        return roots->interior == nullptr ? LxrRcResult::Success :
            engine->RelocateRoot(&roots->interior, true, false);
    }

    bool InitializeMoving(
        LxrReferenceCounting& engine, MovingRoots* roots, size_t blocks, LxrRcMutator** mutator,
        void* identity, uint8_t bits)
    {
        LxrRcConfiguration configuration{
            blocks * ImmixBlockGeometry::BlockBytes, 2 * LxrLargeObjectSpace::MinimumObjectBytes,
            4, 4096, 4, bits,
        };
        configuration.evacuateNursery = true;
        configuration.lineRecycling = false;
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        LxrRcObjectModel model{};
        model.context = roots;
        model.finishRelocation = RelocateMoving;
        return Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success,
            "moving engine initializes") &&
            Check(engine.AttachMutator(identity, mutator) == LxrRcResult::Success,
                "moving allocator attaches");
    }

    void ValidateMatureMovement(uint8_t bits, bool pin)
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        LxrRcConfiguration configuration{
            32 * ImmixBlockGeometry::BlockBytes, 4 * LxrLargeObjectSpace::MinimumObjectBytes,
            4, 4096, 4, bits,
        };
        configuration.evacuateMature = true;
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        LxrRcObjectModel model{};
        model.context = &roots;
        model.finishRelocation = RelocateMoving;
        if (!Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success &&
                engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                "mature engine uses default mutator recycling"))
        {
            return;
        }
        Type type(4096, 1);
        std::vector<Object*> sources;
        for (size_t index = 0; index < 64; index++)
        {
            Object* object = Allocate(engine, mutator, type);
            if (object == nullptr)
            {
                return;
            }
            if (index % 8 == 0)
            {
                sources.push_back(object);
                reinterpret_cast<uint8_t*>(object)[2000] = static_cast<uint8_t>(index + 1);
            }
        }
        for (size_t index = 0; index < sources.size(); index++)
        {
            *reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(sources[index]) + sizeof(uintptr_t)) =
                sources[(index + 1) % sources.size()];
        }
        roots.objects = sources;
        if (pin)
        {
            roots.pinCandidate = sources[0];
        }
        if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "sparse cyclic graph matures without moving"))
        {
            return;
        }
        for (Object* source : sources)
        {
            uintptr_t count;
            Check(engine.LoadCount(source, &count) == LxrRcResult::Success && count != 0,
                "mature source has positive RC before the moving full trace");
        }
        roots.interior = reinterpret_cast<Object*>(reinterpret_cast<uint8_t*>(sources[1]) + 2000);
        if (!Check(engine.CollectFull(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "mature moving full trace and lazy tail finish"))
        {
            return;
        }
        LxrMatureMetrics mature = engine.GetMatureMetrics();
        Check(mature.copiedObjects == (pin ? 7u : 8u) && mature.copiedBytes == (pin ? 7u : 8u) * 4096,
            "actual mature copy volume excludes the pinned source block");
        Check(mature.retiredSourceBytes == mature.copiedBytes &&
                mature.releasedBlocks == (pin ? 7u : 8u),
            "successfully evacuated whole source blocks are really released");
        Check(engine.GetNurseryMetrics().copiedBytes == 0 &&
                engine.GetNurseryMetrics().pauseCopiedBytes == 0,
            "mature movement does not contaminate the nursery predictor");
        for (size_t index = 0; index < sources.size(); index++)
        {
            Object* destination = roots.objects[index];
            Check((destination == sources[index]) == (pin && index == 0),
                "positive-RC survivors move while the late-discovered pin remains stationary");
            Check(reinterpret_cast<uint8_t*>(destination)[2000] == index * 8 + 1,
                "mature destination retains exact payload identity");
            Check(*reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(destination) + sizeof(uintptr_t)) ==
                    roots.objects[(index + 1) % roots.objects.size()],
                "the cyclic heap edge follows the destination");
        }
        Check(roots.interior == reinterpret_cast<Object*>(reinterpret_cast<uint8_t*>(roots.objects[1]) + 2000),
            "mature ordinary interior preserves its exact offset");
        bool reused = false;
        for (size_t index = 0; index < 256 && !reused; index++)
        {
            Object* replacement = Allocate(engine, mutator, type);
            if (replacement == nullptr)
            {
                return;
            }
            for (size_t source = pin ? 1 : 0; source < sources.size(); source++)
            {
                if (replacement == sources[source])
                {
                    reused = true;
                }
            }
        }
        Check(reused, "a subsequent default mutator allocation physically reuses an exact released source address");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "mature mutator detaches");
    }

    void ValidateMatureCopyFailure()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        LxrRcConfiguration configuration{
            3 * ImmixBlockGeometry::BlockBytes, 2 * LxrLargeObjectSpace::MinimumObjectBytes,
            4, 4096, 4, 8,
        };
        configuration.evacuateMature = true;
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        LxrRcObjectModel model{};
        model.context = &roots;
        model.finishRelocation = RelocateMoving;
        if (!Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success &&
                engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                "fixed admitted copy-failure fixture initializes"))
        {
            return;
        }
        Type type(4096, 0);
        for (size_t index = 0; index < 16; index++)
        {
            Object* object = Allocate(engine, mutator, type);
            if (object == nullptr)
            {
                return;
            }
            reinterpret_cast<uint8_t*>(object)[2000] = static_cast<uint8_t>(index + 1);
            roots.objects.push_back(object);
        }
        if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "dense survivors have prior positive RC") ||
            !Check(engine.CollectFull(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "partial mature exhaustion still completes a truthful full trace"))
        {
            return;
        }
        LxrMatureMetrics mature = engine.GetMatureMetrics();
        Check(mature.copiedObjects == 8 && mature.copyFailures == 1 &&
                mature.retiredSourceBytes == 32768 && mature.releasedBlocks == 1,
            "exhaustion retains the unsuccessful source block and releases only the completely evacuated block");
        Check(engine.GetLiveBytes() == 65536 && engine.GetImmixCapacityBytes() == 3 * 32768,
            "copy failure neither loses live bytes nor bypasses admitted capacity");
        for (size_t index = 0; index < roots.objects.size(); index++)
        {
            Object* object = roots.objects[index];
            uintptr_t count;
            Check(engine.GetObjectExtent(object) == 4096 &&
                    engine.LoadCount(object, &count) == LxrRcResult::Success && count == 1 &&
                    object->GetHeader()->GetGCReservedBits(3, 0) == 0 &&
                    reinterpret_cast<uint8_t*>(object)[2000] == index + 1,
                "every copied or retained object finishes with exact RC, extent, cleared claim and payload");
        }
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "copy-failure mutator detaches");
    }

    void ValidateMatureRememberedReuse(
        bool wholeBlock = false, size_t reuses = 1, bool sameShape = false, uint8_t boundary = 0,
        bool releasedCandidate = false)
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        LxrRcConfiguration configuration{
            (releasedCandidate ? 6 : 32) * ImmixBlockGeometry::BlockBytes, 4 * LxrLargeObjectSpace::MinimumObjectBytes,
            4, 4096, 4, 8,
        };
        configuration.evacuateMature = true;
        configuration.defragHeadroomPercent = 50;
        configuration.chunkDefragPercent = 0;
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        LxrRcObjectModel model{};
        model.context = &roots;
        model.finishRelocation = RelocateMoving;
        if (!Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success &&
                engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                "reuse remset engine initializes"))
        {
            return;
        }
        Type sparseType(4096, 0);
        std::vector<Object*> targets;
        for (size_t index = 0; index < 32; index++)
        {
            Object* object = Allocate(engine, mutator, sparseType);
            if (object == nullptr)
            {
                return;
            }
            if (index % 8 == 0)
            {
                targets.push_back(object);
            }
        }
        size_t fieldOffset = boundary == 1 ? 24 : (boundary == 2 ? 32 : sizeof(uintptr_t));
        Type carrierType(4096, 1, fieldOffset);
        Type prefixType(32, 0);
        Type tailType(4064, 0);
        Type shiftedType(4096, 1, 16);
        std::vector<Object*> carriers;
        for (size_t index = 0; index < 8; index++)
        {
            Object* object = Allocate(engine, mutator, carrierType);
            if (object == nullptr)
            {
                return;
            }
            carriers.push_back(object);
        }
        Type largeType(65536, 1);
        Object* large = Allocate(engine, mutator, largeType);
        if (large == nullptr)
        {
            return;
        }
        Object** staleSlot = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(carriers[0]) + fieldOffset);
        Object** legitimateSlot = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(large) + sizeof(uintptr_t));
        Check(engine.WatchRememberedSlot(staleSlot) == LxrRcResult::Success,
            "the payload receipt watches the real scanner-recorded field address");
        *staleSlot = targets[0];
        *legitimateSlot = targets[0];
        roots.objects = carriers;
        roots.objects.push_back(large);
        roots.objects.insert(roots.objects.end(), targets.begin() + 1, targets.end());
        if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "incoming edges and sparse mature targets age") ||
            !Check(engine.HoldTrace(true) == LxrRcResult::Success &&
                engine.BeginTrace(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.HoldTrace(false) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "real concurrent scanner creates the remembered entries"))
        {
            return;
        }
        Check(engine.GetMatureMetrics().rememberedEntries >= 2,
            "real Immix and LOS incoming fields are remembered by tracing");
        roots.objects.erase(roots.objects.begin(), roots.objects.begin() + (wholeBlock ? 8 : 1));
        if (releasedCandidate)
        {
            roots.objects.erase(std::remove(roots.objects.begin(), roots.objects.end(), targets.back()),
                roots.objects.end());
        }
        if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success &&
                engine.GetObjectExtent(carriers[0]) == 0, "the remembered source dies during the same concurrent trace"))
        {
            return;
        }
        if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
                "the next RC sweep publishes the lazily reclaimed carrier hole while SATB remains active"))
        {
            return;
        }
        Object* replacement = Allocate(engine, mutator, sparseType);
        if (wholeBlock)
        {
            for (size_t index = 0; index < 32 * 8 && replacement != carriers[0]; index++)
            {
                replacement = Allocate(engine, mutator, sparseType);
                if (replacement == nullptr)
                {
                    return;
                }
            }
        }
        if (!Check(replacement == carriers[0], "default recycled-line allocation physically replaces the stale source"))
        {
            return;
        }
        if (reuses > 1 && !Check(engine.GetTracePhase() == LxrTracePhase::Concurrent,
                "the actual SATB generation remains live across the byte-wrap reuse sequence"))
        {
            return;
        }
        for (size_t reuse = 1; reuse < reuses; reuse++)
        {
            roots.objects.push_back(replacement);
            if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                    engine.Drain(Timeout) == LxrRcResult::Success,
                    "the replacement really promotes during the same SATB generation"))
            {
                return;
            }
            roots.objects.pop_back();
            if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                    engine.Drain(Timeout) == LxrRcResult::Success &&
                    engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                    engine.Drain(Timeout) == LxrRcResult::Success && engine.GetObjectExtent(replacement) == 0,
                    "the replacement really dies and its exact line becomes reusable"))
            {
                return;
            }
            replacement = Allocate(engine, mutator, sparseType);
            if (!Check(replacement == carriers[0], "each counter-wrap step is real same-line address reuse"))
            {
                return;
            }
        }
        *staleSlot = targets[0];
        roots.objects.push_back(replacement);
        if (releasedCandidate)
        {
            Check(engine.GetObjectExtent(targets.back()) == 0,
                "an actual selected block was released while the same SATB generation remained active");
            for (size_t index = 0; index < 8; index++)
            {
                Object* filler = Allocate(engine, mutator, sparseType);
                if (filler == nullptr)
                {
                    return;
                }
                roots.objects.push_back(filler);
            }
        }
        if (sameShape || boundary != 0)
        {
            roots.objects.pop_back();
            if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                    engine.Drain(Timeout) == LxrRcResult::Success &&
                    engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                    engine.Drain(Timeout) == LxrRcResult::Success,
                    "the last primitive replacement retires before same-shape reuse"))
            {
                return;
            }
            replacement = Allocate(engine, mutator,
                sameShape ? carrierType : (boundary == 4 ? shiftedType : prefixType));
            if (!Check(replacement == carriers[0],
                    "the final replacement starts at the exact remembered source address"))
            {
                return;
            }
            roots.objects.push_back(replacement);
            if (sameShape)
            {
                Check(Store(engine, mutator, staleSlot, targets[0]),
                    "a same-shape replacement publishes a legitimate current incoming field");
            }
            else if (boundary == 1 || boundary == 2)
            {
                Object* tail = Allocate(engine, mutator, tailType);
                if (!Check(tail != nullptr &&
                        reinterpret_cast<uintptr_t>(tail) == reinterpret_cast<uintptr_t>(replacement) + 32,
                        "real aligned prefix and tail allocations put the old field at a header boundary"))
                {
                    return;
                }
                tail->GetHeader()->SetBit(BIT_SBLK_FINALIZER_RUN);
                roots.objects.push_back(tail);
            }
            else if (boundary == 4)
            {
                *staleSlot = targets[0];
                Check(Store(engine, mutator,
                    reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(replacement) + 16), targets[0]),
                    "the replacement has a genuine current reference field at a different interior offset");
            }
        }
        if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
                "the poisoned primitive replacement is promoted before final remset validation"))
        {
            return;
        }
        if (boundary == 3)
        {
            roots.objects.pop_back();
            if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                    engine.Drain(Timeout) == LxrRcResult::Success &&
                    engine.GetObjectExtent(replacement) == 0,
                    "the replacement owner actually retires before its dead layout is poisoned"))
            {
                return;
            }
            replacement->RawSetMethodTable(reinterpret_cast<MethodTable*>(static_cast<uintptr_t>(1)));
            *staleSlot = targets[0];
        }
        uintptr_t expectedPayload = *reinterpret_cast<uintptr_t*>(staleSlot);
        if (!Check(engine.FinishTrace(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "final mark replays the surviving incoming edge"))
        {
            return;
        }
        Check(sameShape ? *staleSlot == *legitimateSlot :
                *reinterpret_cast<uintptr_t*>(staleSlot) == expectedPayload,
            "stale primitive payload is untouched, while a current genuine reference field follows its target");
        Check(*legitimateSlot != targets[0] && engine.GetObjectExtent(*legitimateSlot) == 4096,
            "the legitimate LOS incoming field follows the actually moved mature target");
        LxrMatureMetrics mature = engine.GetMatureMetrics();
        Check((sameShape || mature.staleEntries + mature.invalidSlots >= 1) &&
                mature.replayedEntries >= 1 && mature.copiedObjects == (releasedCandidate ? 3u : 4u),
            "stale rejection and actual incoming-edge replay both occur");
        Check(sameShape ? engine.GetRememberedPayloadReads() >= 2 : engine.GetRememberedPayloadReads() == 0,
            "the actual replay never reads a primitive stale payload, and safe current-field duplicates replay");
        if (sameShape)
        {
            uintptr_t count;
            Check(engine.LoadCount(*legitimateSlot, &count) == LxrRcResult::Success && count == 2,
                "duplicate current-field entries neither double increment RC nor duplicate mature copies");
        }
        if (boundary == 4)
        {
            Check(*reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(replacement) + 16) == *legitimateSlot,
                "current GCDesc offsets repair the real incoming edge without reading the old scalar offset");
        }
        else if (reuses == 1)
        {
            Check(mature.staleEntries >= 1 && mature.invalidSlots == 0,
                "the ordinary changed reuse tag rejects before current-owner shape enumeration");
        }
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "remset mutator detaches");
    }

    void ValidateRetainedMatureOwner(bool pin)
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        LxrRcConfiguration configuration{
            3 * ImmixBlockGeometry::BlockBytes, 2 * LxrLargeObjectSpace::MinimumObjectBytes,
            4, 4096, 4, 8,
        };
        configuration.evacuateMature = true;
        configuration.defragHeadroomPercent = 50;
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        LxrRcObjectModel model{};
        model.context = &roots;
        model.finishRelocation = RelocateMoving;
        if (!Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success &&
                engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                "retained mature owner engine initializes"))
        {
            return;
        }
        Type ownerType(24000, 1);
        Type targetType(24000, 0);
        Object* owner = Allocate(engine, mutator, ownerType);
        Object* target = Allocate(engine, mutator, targetType);
        if (owner == nullptr || target == nullptr)
        {
            return;
        }
        Object** slot = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(owner) + sizeof(uintptr_t));
        *slot = target;
        reinterpret_cast<uint8_t*>(target)[2000] = 71;
        roots.objects = {target, owner};
        if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success &&
                engine.BeginTrace(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success &&
                engine.IsMarked(owner) && engine.IsMarked(target),
                "both selected mature owners finish real concurrent scanning before final mark"))
        {
            return;
        }
        if (pin)
        {
            roots.pinCandidate = owner;
        }
        if (!Check(engine.FinishTrace(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
                "late pin or partial copy exhaustion finishes ordinary final-mark closure"))
        {
            return;
        }
        Check(roots.objects[1] == owner && roots.objects[0] != target &&
                *slot == roots.objects[0] && engine.GetObjectExtent(target) == 0,
                "a previously scanned retained owner repairs its selected-to-selected edge before source retirement");
        Check(reinterpret_cast<uint8_t*>(*slot)[2000] == 71,
            "the retained owner observes the copied logical target, not retired payload");
        Check(pin ? engine.GetMatureMetrics().pinnedBlocks == 1 :
                engine.GetMatureMetrics().copyFailures == 1,
                "retention is caused by the actual late pin or real admitted to-space exhaustion");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "retained owner mutator detaches");
    }

    void ValidateMatureMovingReader()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        LxrRcConfiguration configuration{
            16 * ImmixBlockGeometry::BlockBytes, 2 * LxrLargeObjectSpace::MinimumObjectBytes,
            4, 4096, 4, 8,
        };
        configuration.evacuateMature = true;
        configuration.defragHeadroomPercent = 50;
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        LxrRcObjectModel model{};
        model.context = &roots;
        model.finishRelocation = RelocateMoving;
        if (!Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success &&
                engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                "mature reader engine initializes"))
        {
            return;
        }
        engine.SetTraceLoopForTest(false);
        Type carrierType(24000, 2500);
        Type targetType(4096, 0);
        Object* carrier = Allocate(engine, mutator, carrierType);
        Object* source = Allocate(engine, mutator, targetType);
        if (carrier == nullptr || source == nullptr)
        {
            return;
        }
        Object** fields = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(carrier) + sizeof(uintptr_t));
        fields[0] = fields[2050] = source;
        reinterpret_cast<uint8_t*>(source)[2000] = 91;
        roots.objects = {carrier};
        roots.interior = reinterpret_cast<Object*>(reinterpret_cast<uint8_t*>(carrier) + 22000);
        if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "both reader owners genuinely age before marking") ||
            !Check(engine.HoldTrace(true) == LxrRcResult::Success &&
                engine.ArmScanCheckpoint(carrier) == LxrRcResult::Success &&
                engine.BeginTrace(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.ArmPendingReader(source) == LxrRcResult::Success &&
                engine.HoldTrace(false) == LxrRcResult::Success,
                "a real resumable scanner emits a selected packet naming a positive-RC mature source"))
        {
            return;
        }
        std::chrono::steady_clock::time_point deadline = std::chrono::steady_clock::now() +
            std::chrono::milliseconds(Timeout);
        while ((engine.GetPendingReaderId() == 0 || !engine.IsScanCheckpointActive()) &&
            std::chrono::steady_clock::now() < deadline)
        {
            std::this_thread::yield();
        }
        if (!Check(engine.GetPendingReaderId() != 0 && engine.IsScanCheckpointActive() &&
                engine.GetPendingReaderInitialValue() == reinterpret_cast<uintptr_t>(source) &&
                engine.GetPendingReaderCompletions() == 0,
                "the actual GCDesc packet and active interrupted iterator still own old mature addresses"))
        {
            engine.StopWorkers();
            return;
        }
        // Keep the next real continuation unpublished before RunPause releases the active checkpoint.
        if (!Check(
#ifndef LXR_MATURE_READER_COMPLETION_CONTROL
                engine.HoldTracePacketsForTest(true) == LxrRcResult::Success &&
#endif // !LXR_MATURE_READER_COMPLETION_CONTROL
                engine.FinishTrace(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
                "ordinary final-mark closure moves mature owners and drains their actual readers"))
        {
            return;
        }
        Object* moved = roots.objects[0];
        Object** movedFields = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(moved) + sizeof(uintptr_t));
        Object* destination = movedFields[0];
        Check(moved != carrier && destination != source && movedFields[2050] == destination &&
                reinterpret_cast<uint8_t*>(destination)[2000] == 91,
                "both early and continuation fields follow the copied mature target");
        std::printf("mature_reader_receipt source=%zx expected_source=%zx destination=%zx expected_destination=%zx "
            "range=%zx expected_range=%zx users=%zu\n",
            static_cast<size_t>(engine.GetMatureScanReceipt(0)), reinterpret_cast<size_t>(carrier),
            static_cast<size_t>(engine.GetMatureScanReceipt(1)), reinterpret_cast<size_t>(moved),
            static_cast<size_t>(engine.GetMatureScanReceipt(2)), reinterpret_cast<size_t>(movedFields),
            static_cast<size_t>(engine.GetMatureScanReceipt(3)));
        Check(engine.GetMatureScanReceipt(0) == reinterpret_cast<uintptr_t>(carrier) &&
                engine.GetMatureScanReceipt(1) == reinterpret_cast<uintptr_t>(moved) &&
                engine.GetMatureScanReceipt(2) == reinterpret_cast<uintptr_t>(movedFields) &&
                engine.GetMatureScanReceipt(3) != 0,
                "an actual user-owned resumable descriptor rebases its range before source reuse");
        Check(engine.GetMatureScanReceipt(4) == 1 && engine.GetMatureScanReceipt(5) == 0,
            "the index of split scans holds the scan under the address of the copy and no longer under the source");
        Check(engine.GetPendingReaderCompletions() == 1 &&
                engine.GetObjectExtent(carrier) == 0 && engine.GetObjectExtent(source) == 0 &&
                roots.interior == reinterpret_cast<Object*>(reinterpret_cast<uint8_t*>(moved) + 22000),
                "the real packet consumes once while sources remain valid, then retirement and interiors finish");
        Object* replacement = nullptr;
        Type prefixType(24000, 0);
        for (size_t attempt = 0; attempt < 16 && replacement != carrier; attempt++)
        {
            replacement = Allocate(engine, mutator, prefixType);
        }
        Object* targetReplacement = Allocate(engine, mutator, targetType);
        Check(replacement == carrier && targetReplacement == source,
            "both evacuated exact addresses become real subsequent mutator allocations");
        if (targetReplacement != nullptr)
        {
            reinterpret_cast<uint8_t*>(targetReplacement)[2000] = 173;
        }
        Check(engine.GetPendingReaderCompletions() == 1 &&
                reinterpret_cast<uint8_t*>(destination)[2000] == 91,
                "physical replacements cannot mask reader identity or cause a second packet consumption");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "mature reader mutator detaches");
    }

    void ValidateCompletedMatureScan()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        LxrRcConfiguration configuration{
            16 * ImmixBlockGeometry::BlockBytes, 2 * LxrLargeObjectSpace::MinimumObjectBytes,
            4, 4096, 4, 8,
        };
        configuration.evacuateMature = true;
        configuration.defragHeadroomPercent = 50;
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        LxrRcObjectModel model{};
        model.context = &roots;
        model.finishRelocation = RelocateMoving;
        if (!Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success &&
                engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                "completed-scan engine initializes"))
        {
            return;
        }
        Type prefixType(256, 0);
        Type carrierType(24000, 2500);
        Type replacementType(28000, 0);
        Object* prefix = Allocate(engine, mutator, prefixType);
        Object* carrier = Allocate(engine, mutator, carrierType);
        if (prefix == nullptr || carrier == nullptr)
        {
            return;
        }
        roots.objects = {carrier};
        if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success &&
                engine.ArmScanCheckpoint(carrier) == LxrRcResult::Success &&
                engine.BeginTrace(CaptureMoving, &roots, Timeout) == LxrRcResult::Success,
                "a real descriptor scans a non-block-start mature owner"))
        {
            return;
        }
        std::chrono::steady_clock::time_point deadline = std::chrono::steady_clock::now() +
            std::chrono::milliseconds(Timeout);
        while (!engine.IsScanCheckpointActive() && std::chrono::steady_clock::now() < deadline)
        {
            std::this_thread::yield();
        }
        if (!Check(engine.IsScanCheckpointActive() &&
                engine.HoldTracePacketsForTest(true) == LxrRcResult::Success,
                "the real continuation retains its descriptor through the existing publication gate"))
        {
            engine.StopWorkers();
            return;
        }
        roots.objects.clear();
        if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success &&
                engine.GetObjectExtent(carrier) == 0 && engine.GetCompletedScanUsers(carrier) != 0,
                "mark-before-death completes the actual descriptor while a real continuation still owns it"))
        {
            return;
        }
        Object* replacement = nullptr;
        for (size_t attempt = 0; attempt < 16 && replacement != prefix; attempt++)
        {
            replacement = Allocate(engine, mutator, replacementType);
            if (replacement == nullptr)
            {
                return;
            }
        }
        if (!Check(replacement == prefix, "whole-block allocation puts a larger real object over the dead owner"))
        {
            return;
        }
        roots.objects.push_back(replacement);
        if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
                "the replacement promotes before the completed descriptor is consumed"))
        {
            return;
        }
        uintptr_t raw = 0;
        Check(engine.GetObjectExtent(carrier) == 0 &&
                engine.LoadCount(carrier, &raw) == LxrRcResult::Success && raw == 1,
                "the former exact start is now an unregistered continuation-line sentinel, not an object");
        Check(engine.CollectFull(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success &&
                engine.GetMatureMetrics().copyFailures == 0 && engine.GetLiveBytes() == 28000,
                "completed dead-owner work finishes without reading interior headers or attempting zero-size copy");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "completed-scan mutator detaches");
    }

    void ValidateNurseryHeadroom()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        LxrRcConfiguration configuration{
            64 * ImmixBlockGeometry::BlockBytes, 2 * LxrLargeObjectSpace::MinimumObjectBytes,
            4, 4096, 4, 8,
        };
        configuration.evacuateMature = true;
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        if (!Check(engine.Initialize(configuration, adapter) == LxrRcResult::Success &&
                engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                "nursery headroom engine initializes"))
        {
            return;
        }
        Type type(24000, 0);
        for (size_t index = 0; index < 64; index++)
        {
            Object* object = Allocate(engine, mutator, type);
            if (object == nullptr)
            {
                return;
            }
            roots.objects.push_back(object);
        }
        size_t headroom = 0;
        size_t used = 0;
        Check(engine.GetUsedPages(&used) == LxrRcResult::Success && used == 512 &&
                engine.GetDefragHeadroomBytes(&headroom) == LxrRcResult::Success && headroom == 25 * 4096,
                "allocated nursery blocks contribute real reserved pages before their block state is promoted");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "headroom mutator detaches");
    }

    void ValidateMovingReader()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator;
        MovingRoots roots;
        int identity;
        if (!InitializeMoving(engine, &roots, 16, &mutator, &identity, 8))
        {
            return;
        }
        engine.SetTraceLoopForTest(false);
        Type carrierType(65536, 4097);
        Object* carrier = Allocate(engine, mutator, carrierType);
        roots.objects = {carrier};
        if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "reader carrier matures before tracing") ||
            !Check(engine.HoldTrace(true) == LxrRcResult::Success &&
                engine.ArmScanCheckpoint(carrier) == LxrRcResult::Success &&
                engine.BeginTrace(CaptureMoving, &roots, Timeout) == LxrRcResult::Success,
                "real resumable carrier tracing starts behind its publication gate"))
        {
            return;
        }
        Type targetType(24576, 0);
        Object* source = Allocate(engine, mutator, targetType);
        reinterpret_cast<uint8_t*>(source)[10000] = 91;
        uintptr_t count;
        Check(engine.LoadCount(source, &count) == LxrRcResult::Success && count == 0,
            "the reader target is a real fresh RC-zero birth after InitialMark");
        Object** field = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(carrier) + sizeof(uintptr_t));
        if (!Check(engine.ArmPendingReader(source) == LxrRcResult::Success &&
                Store(engine, mutator, field, source) &&
                engine.HoldTrace(false) == LxrRcResult::Success, "the real scanner will encounter the armed target"))
        {
            return;
        }
        std::chrono::steady_clock::time_point deadline = std::chrono::steady_clock::now() +
            std::chrono::milliseconds(Timeout);
        while ((engine.GetPendingReaderId() == 0 || !engine.IsScanCheckpointActive()) &&
            std::chrono::steady_clock::now() < deadline)
        {
            std::this_thread::yield();
        }
        uint64_t id = engine.GetPendingReaderId();
        if (!Check(id != 0 && engine.IsScanCheckpointActive() &&
                engine.GetPendingReaderInitialValue() == reinterpret_cast<uintptr_t>(source) &&
                engine.GetPendingReaderValue() == reinterpret_cast<uintptr_t>(source) &&
                engine.GetPendingReaderCompletions() == 0,
                "an actual GCDesc slice emitted an owned, unsubmitted packet naming the zero-RC source"))
        {
            engine.StopWorkers();
            return;
        }
        uint64_t copied = engine.GetNurseryMetrics().copiedBytes;
        Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success,
            "RC interrupts the real scan while its source-bearing child packet is held");
        Object* destination = *field;
        Check(destination != source && engine.GetNurseryMetrics().copiedBytes - copied == 24576 &&
            engine.GetObjectExtent(source) == 0 && engine.GetPendingReaderCompletions() == 0,
            "real copy and source retirement precede consumption of the held reader");
        Object* replacement = nullptr;
        for (size_t attempt = 0; attempt < 16; attempt++)
        {
            Object* value = Allocate(engine, mutator, targetType);
            if (value == source)
            {
                replacement = value;
                reinterpret_cast<uint8_t*>(replacement)[10000] = 173;
                break;
            }
        }
        Check(replacement == source && engine.GetObjectExtent(replacement) == 24576 &&
            engine.LoadCount(replacement, &count) == LxrRcResult::Success && count == 0,
            "the exact retired source is a distinct physical replacement before the reader resumes");
        Check(engine.ReleasePendingReader() == LxrRcResult::Success,
            "the selected reader resumes through ordinary scheduler publication");
        deadline = std::chrono::steady_clock::now() + std::chrono::milliseconds(Timeout);
        while ((!engine.IsTraceExhausted() || engine.GetPendingReaderCompletions() == 0) &&
            std::chrono::steady_clock::now() < deadline)
        {
            std::this_thread::yield();
        }
        Check(engine.GetPendingReaderId() == id && engine.GetPendingReaderCompletions() == 1 &&
            engine.GetPendingReaderConsumedValue() == reinterpret_cast<uintptr_t>(destination) &&
            engine.IsTraceExhausted(),
            "the same actual packet dispatches the destination exactly once after retirement and exact reuse");
        Check(*field == destination && reinterpret_cast<uint8_t*>(destination)[10000] == 91 &&
            reinterpret_cast<uint8_t*>(replacement)[10000] == 173,
            "reader identity cannot be masked by newborn black marking or replacement payload");
        Check(engine.FinishTrace(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
            engine.Drain(Timeout) == LxrRcResult::Success, "the interrupted moving trace completes normally");
    }

    void ValidateMovingNursery(uint8_t bits)
    {
        {
            LxrReferenceCounting engine;
            LxrRcMutator* mutator;
            MovingRoots roots;
            int identity;
            if (!InitializeMoving(engine, &roots, 16, &mutator, &identity, bits))
            {
                return;
            }
            Type type(24, 1);
            Object* first = Allocate(engine, mutator, type);
            Object* second = Allocate(engine, mutator, type);
            if (first == nullptr || second == nullptr)
            {
                return;
            }
            *reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(first) + sizeof(uintptr_t)) = second;
            *reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(second) + sizeof(uintptr_t)) = first;
            first->GetHeader()->SetBit(BIT_SBLK_FINALIZER_RUN);
            Check(engine.ArmLifetimeProbe(first) == LxrRcResult::Success,
                "logical source identity is watched without adding a root");
            roots.objects.assign(1025, first);
            Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "duplicate-root copy race drains");
            Object* moved = roots.objects[0];
            Object* child = *reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(moved) + sizeof(uintptr_t));
            Check(moved != first && child != second && child != moved,
                "ordinary roots and both cycle members genuinely move");
            Check(std::all_of(roots.objects.begin(), roots.objects.end(), [moved](Object* object)
                { return object == moved; }), "all racing aliases converge to one destination");
            Check(*reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(child) + sizeof(uintptr_t)) == moved,
                "copied cycle fields point to destinations, not evacuated sources");
            Check(moved->GetGCSafeMethodTable() == type.table &&
                (moved->GetHeader()->GetBits() & BIT_SBLK_FINALIZER_RUN) != 0 &&
                moved->GetHeader()->GetGCReservedBits(3, 0) == 0,
                "MethodTable, synchronization state and clear destination forwarding state survive");
            LxrNurseryMetrics copying = engine.GetNurseryMetrics();
            Check(copying.copiedObjects == 2 && copying.copiedBytes == 48 &&
                copying.pauseCopiedBytes == 48 && copying.retiredSourceBytes == 48,
                "only two physical copies feed actual survival and source retirement");
            Check(engine.GetPauseSurvivorBytes() == 48,
                "copied objects count as evacuable survivors, which size the next to-space reserve");
            uintptr_t count;
            Check(engine.LoadCount(moved, &count) == LxrRcResult::Success &&
                count == (uintptr_t{1} << bits) - 1 &&
                engine.LoadCount(child, &count) == LxrRcResult::Success && count == 1,
                "forwarding losers increment exactly once; nursery graph scans exactly once");
            Check(engine.GetObjectExtent(first) == 0 && engine.GetObjectExtent(second) == 0,
                "forwarded physical sources are no longer valid object starts");
            Type filler(24576, 0);
            Object* replacement = nullptr;
            for (size_t attempt = 0; attempt < 15; attempt++)
            {
                Object* value = Allocate(engine, mutator, filler);
                if (value == first)
                {
                    replacement = value;
                    break;
                }
            }
            Check(replacement == first && replacement->GetHeader()->GetGCReservedBits(3, 0) == 0,
                "an evacuated source is actually reused with clear forwarding state");
            // P28 (F9). The block cursors start at 0 after a pause, so the first allocation takes the freed source block. The test needs
            // the heap to be full when the next pause runs (the replacement is then promoted in place, not copied): fill it.
            for (size_t attempt = 0; attempt < 32; attempt++)
            {
                Object* ignored = nullptr;
                if (engine.Allocate(mutator, filler.table->GetBaseSize(), &ignored) != LxrRcResult::Success)
                {
                    break;
                }
            }
            if (replacement != nullptr)
            {
                roots.objects.push_back(replacement);
            }
            Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success && roots.objects[0] == moved &&
                engine.GetNurseryMetrics().copiedBytes == 48 &&
                engine.GetNurseryMetrics().pauseCopiedBytes == 0,
                "mature objects remain stationary and the next epoch has no fake copied survival");
            Check(engine.GetLifetimeProbeReclaims() == 0 && engine.GetLifetimeProbeBirths() == 0 &&
                *reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(child) + sizeof(uintptr_t)) == moved,
                "same-source replacement cannot masquerade as or retire the copied logical identity");
            roots.objects = {replacement};
            Check(engine.CollectFull(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success &&
                engine.GetLifetimeProbeReclaims() == 1 && engine.GetObjectExtent(replacement) == 24576,
                "the original logical identity retires while its exact old address still contains a live replacement");
        }
        for (bool pinFirst : {false, true})
        {
            LxrReferenceCounting engine;
            LxrRcMutator* mutator;
            MovingRoots roots;
            int identity;
            if (!InitializeMoving(engine, &roots, 16, &mutator, &identity, bits))
            {
                return;
            }
            Type tiny(24, 1);
            Type large(20480, 0);
            roots.pinned = Allocate(engine, mutator, tiny);
            Allocate(engine, mutator, large);
            Object* movable = Allocate(engine, mutator, large);
            if (roots.pinned == nullptr || movable == nullptr)
            {
                return;
            }
            uintptr_t pinnedBlock = reinterpret_cast<uintptr_t>(roots.pinned) & ~ImmixBlockGeometry::BlockMask;
            *reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(roots.pinned) + sizeof(uintptr_t)) = movable;
            reinterpret_cast<uint8_t*>(movable)[10000] = 91;
            roots.interior = reinterpret_cast<Object*>(reinterpret_cast<uint8_t*>(movable) + 10000);
            roots.objects = {roots.pinned};
            roots.pinCandidate = roots.pinned;
            roots.pinned = nullptr;
            roots.pinFirst = pinFirst;
            LxrRcResult collected = engine.Collect(CaptureMoving, &roots, Timeout);
            LxrRcResult drained = collected == LxrRcResult::Success ? engine.Drain(Timeout) : collected;
            if (drained != LxrRcResult::Success)
            {
                std::fprintf(stderr, "mixed result=%u failure=%u copied=%llu retired=%llu\n",
                    static_cast<unsigned>(collected), static_cast<unsigned>(engine.GetFailure()),
                    static_cast<unsigned long long>(engine.GetNurseryMetrics().copiedBytes),
                    static_cast<unsigned long long>(engine.GetNurseryMetrics().retiredSourceBytes));
            }
            Check(drained == LxrRcResult::Success, "mixed pinned/interior closure drains");
            Object* moved = *reinterpret_cast<Object**>(
                reinterpret_cast<uint8_t*>(roots.objects[0]) + sizeof(uintptr_t));
            Check(roots.objects[0] == roots.pinned && moved != movable &&
                reinterpret_cast<uintptr_t>(roots.pinned) >= pinnedBlock &&
                engine.GetObjectExtent(roots.pinned) == 24,
                "a partially pinned source block retains its owner while other blocks evacuate");
            Check(roots.interior == reinterpret_cast<Object*>(reinterpret_cast<uint8_t*>(moved) + 10000) &&
                *reinterpret_cast<uint8_t*>(roots.interior) == 91,
                "a relocatable interior beyond a page preserves its exact object-relative offset and payload");
            Check(engine.GetNurseryMetrics().inPlacePinnedBytes == 24 &&
                engine.GetNurseryMetrics().copiedBytes == 20480,
                "pinned in-place promotion is separate from genuine copied volume");
            Check(engine.GetPauseSurvivorBytes() == 20480 + 24,
                "copied and in-place nursery survivors both size the to-space reserve");
        }
        {
            LxrReferenceCounting engine;
            LxrRcMutator* mutator;
            MovingRoots roots;
            int identity;
            if (!InitializeMoving(engine, &roots, 2, &mutator, &identity, bits))
            {
                return;
            }
            Type anchorType(24, 0);
            roots.pinned = Allocate(engine, mutator, anchorType);
            roots.objects = {roots.pinned};
            Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "a pinned anchor establishes an available mature hole");
            Type overflowType(8192, 0);
            Object* overflow = Allocate(engine, mutator, overflowType);
            roots.objects.push_back(overflow);
            Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "multiline copy overflow handles absence of fresh blocks");
            Check(roots.objects[1] == overflow && engine.GetNurseryMetrics().copiedBytes == 0 &&
                engine.GetNurseryMetrics().copyFailures == 1,
                "multiline overflow must not substitute a recycled mature hole for the oracle's fresh cursor");
        }
        {
            LxrReferenceCounting engine;
            LxrRcMutator* mutator;
            MovingRoots roots;
            int identity;
            if (!InitializeMoving(engine, &roots, 2, &mutator, &identity, bits))
            {
                return;
            }
            Type type(24576, 0);
            Object* first = Allocate(engine, mutator, type);
            Object* second = Allocate(engine, mutator, type);
            roots.objects = {first, second};
            Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "to-space exhaustion completes without aborting a claim");
            Check(roots.objects[0] == first && roots.objects[1] == second &&
                engine.GetNurseryMetrics().copyFailures != 0 &&
                engine.GetNurseryMetrics().copiedBytes == 0 &&
                first->GetHeader()->GetGCReservedBits(3, 0) == 0 &&
                second->GetHeader()->GetGCReservedBits(3, 0) == 0,
                "copy failure retains both complete objects and clears every claim");
            roots.objects.clear();
            Check(engine.CollectFull(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "fallback survivors can later be reclaimed");
            Object* next = Allocate(engine, mutator, type);
            roots.objects = {next};
            LxrRcResult collected = engine.Collect(CaptureMoving, &roots, Timeout);
            LxrRcResult drained = collected == LxrRcResult::Success ? engine.Drain(Timeout) : collected;
            if (drained != LxrRcResult::Success || roots.objects[0] == next)
            {
                std::fprintf(stderr, "reuse result=%u failure=%u copied=%llu overflow=%llu\n",
                    static_cast<unsigned>(collected), static_cast<unsigned>(engine.GetFailure()),
                    static_cast<unsigned long long>(engine.GetNurseryMetrics().copiedBytes),
                    static_cast<unsigned long long>(engine.GetNurseryMetrics().copyFailures));
            }
            Check(drained == LxrRcResult::Success && roots.objects[0] != next &&
                engine.GetNurseryMetrics().copiedBytes == 24576,
                "copy overflow is reset for the next epoch after actual source reuse");
        }
    }

    void ValidateLifetimeProbe()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator;
        int identity;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Type type(65536, 0);
        Object* original = Allocate(engine, mutator, type);
        if (original == nullptr)
        {
            return;
        }
        Check(engine.ArmLifetimeProbe(nullptr) == LxrRcResult::InvalidArgument,
            "lifetime probe rejects an absent allocation");
        Check(engine.ArmLifetimeProbe(original) == LxrRcResult::Success &&
            engine.GetLifetimeProbeBirths() == 0 && engine.GetLifetimeProbeReclaims() == 0,
            "single-address probe starts without rooting or counting the original birth");
        Check(engine.ArmLifetimeProbe(original) == LxrRcResult::InvalidPhase,
            "a probe cannot be rearmed across concurrent event observers");
        Epoch(engine, {original});
        Check(engine.GetLifetimeProbeReclaims() == 0, "a retained original has no reclamation event");
        Check(engine.CollectFull(nullptr, nullptr, Timeout) == LxrRcResult::Success &&
            engine.Drain(Timeout) == LxrRcResult::Success &&
            engine.GetObjectExtent(original) == 0 && engine.GetLifetimeProbeReclaims() == 1,
            "a non-rooting probe records retirement after all required readers drain");
        Object* replacement = Allocate(engine, mutator, type);
        if (replacement == nullptr)
        {
            return;
        }
        Check(replacement == original && engine.GetObjectExtent(original) == 65536,
            "a retained same-type replacement makes the historical extent check false");
        Epoch(engine, {replacement});
        Check(engine.GetLifetimeProbeBirths() == 1 && engine.GetLifetimeProbeReclaims() == 1,
            "same-address reuse does not erase the original retirement event");
    }

    void ValidateLogStorage()
    {
        {
            LxrReferenceCounting engine;
            LxrRcMutator* mutator;
            int identity;
            if (!Initialize(engine, 8, &mutator, &identity))
            {
                return;
            }
            size_t payload = engine.GetAdmittedBytes();
            Check(engine.GetStorageMetric(11) == payload && engine.GetStorageMetric(12) == 0 &&
                engine.GetStorageMetric(13) == payload && engine.GetStorageMetric(14) == 0 &&
                engine.GetStorageMetric(17) == payload && engine.GetStorageMetric(18) == 0 &&
                engine.GetStorageMetric(19) == payload && engine.GetStorageMetric(20) == 0,
                "four full-capacity payload streams have no separate publication backing");
            Check(engine.ValidateLogStorageForTest(0) == LxrRcResult::CapacityExceeded &&
                engine.GetFailure() == LxrRcResult::CapacityExceeded,
                "all1024 partial widths and every admitted one-entry flush preserve exact47-bit payload before overflow");
        }
        for (uint8_t test : {uint8_t{1}, uint8_t{2}, uint8_t{3}, uint8_t{4}})
        {
            LxrReferenceCounting engine;
            LxrRcMutator* mutator;
            int identity;
            if (!Initialize(engine, 8, &mutator, &identity))
            {
                return;
            }
            Check(engine.ValidateLogStorageForTest(test) == LxrRcResult::InvariantViolation,
                "holes, foreign high bits, corrupt counts and boundary overshoot fail explicitly");
        }
        {
            LxrReferenceCounting observer;
            Check(observer.InitializeObserver(8, 1) == LxrRcResult::Success &&
                observer.ValidateLogStorageForTest(6) == LxrRcResult::Success,
                "observer publication preserves full-width foreign values without truncation");
        }
        for (uint8_t bits : {uint8_t{2}, uint8_t{4}, uint8_t{8}})
        {
            LxrReferenceCounting engine;
            LxrRcMutator* mutator;
            int identity;
            if (!Initialize(engine, bits, &mutator, &identity))
            {
                return;
            }
            Type ownerType(32, 2);
            Type leafType(24, 0);
            Object* owner = Allocate(engine, mutator, ownerType);
            Object* first = Allocate(engine, mutator, leafType);
            Object* second = Allocate(engine, mutator, leafType);
            if (owner == nullptr || first == nullptr || second == nullptr)
            {
                return;
            }
            Object** fields = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(owner) + sizeof(void*));
            if (!Store(engine, mutator, fields, first) || !Store(engine, mutator, fields + 1, second) ||
                !Epoch(engine, {owner}) || !Store(engine, mutator, fields, nullptr))
            {
                return;
            }
            std::vector<Object*> objects{owner};
            Roots roots{&objects};
            engine.HoldLogReaderForTest(true);
            LxrRcResult collected = engine.Collect(Capture, &roots, Timeout);
            std::chrono::steady_clock::time_point deadline =
                std::chrono::steady_clock::now() + std::chrono::milliseconds(Timeout);
            while (collected == LxrRcResult::Success && !engine.IsLogReaderHeldForTest() &&
                std::chrono::steady_clock::now() < deadline)
            {
                GCToOSInterface::YieldThread(0);
            }
            bool held = Check(collected == LxrRcResult::Success && engine.IsLogReaderHeldForTest(),
                "actual lazy decrement reader retains the previous bank while mutators resume");
            bool stored = held && Store(engine, mutator, fields + 1, nullptr) &&
                Check(engine.DetachMutator(mutator) == LxrRcResult::Success,
                    "next-epoch partial detach publishes while the old reader owns its payload");
            engine.HoldLogReaderForTest(false);
            if (!held || !stored)
            {
                return;
            }
            Check(engine.Drain(Timeout) == LxrRcResult::Success &&
                engine.GetObjectExtent(first) == 0 && engine.GetObjectExtent(second) == 24,
                "old RC consumes its own target exactly once, not the next-epoch target");
            Check(engine.Collect(Capture, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success && engine.GetObjectExtent(second) == 0,
                "next-epoch target retires only after its independent reader drains");
        }
        {
            LxrReferenceCounting engine;
            LxrRcConfiguration configuration{
                4 * ImmixBlockGeometry::BlockBytes, LxrLargeObjectSpace::MinimumObjectBytes,
                1024, 1024, 4, 8,
            };
            LxrPauseRuntimeAdapter runtime{Suspend, Resume, nullptr, false};
            if (!Check(engine.Initialize(configuration, runtime) == LxrRcResult::Success,
                "simultaneous-context fixture admits all log backing before stores"))
            {
                return;
            }
            std::vector<int> identities(1025);
            std::vector<LxrRcMutator*> mutators(1024);
            for (size_t index = 0; index < mutators.size(); index++)
            {
                if (!Check(engine.AttachMutator(&identities[index], &mutators[index]) == LxrRcResult::Success,
                    "all1024 simultaneous logical contexts attach without a per-epoch identity budget"))
                {
                    return;
                }
            }
            LxrRcMutator* excess = nullptr;
            Check(engine.AttachMutator(&identities[1024], &excess) == LxrRcResult::CapacityExceeded &&
                excess == nullptr, "only the documented simultaneous-context limit is enforced");
            Type ownerType(16 + 1024 * sizeof(void*), 1024);
            Type leafType(24, 0);
            Object* owner = Allocate(engine, mutators[0], ownerType);
            Object* leaf = Allocate(engine, mutators[0], leafType);
            Object* replacement = Allocate(engine, mutators[0], leafType);
            if (owner == nullptr || leaf == nullptr || replacement == nullptr)
            {
                return;
            }
            Object** fields = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(owner) + sizeof(void*));
            for (size_t index = 0; index < mutators.size(); index++)
            {
                if (!Store(engine, mutators[0], fields + index, leaf))
                {
                    return;
                }
            }
            if (!Epoch(engine, {owner, leaf, replacement}))
            {
                return;
            }
            LxrRcMetrics before = engine.GetMetrics();
            for (size_t index = 0; index < mutators.size(); index++)
            {
                if (!Store(engine, mutators[index], fields + index, replacement))
                {
                    return;
                }
            }
            LxrRcMetrics logged = engine.GetMetrics();
            Check(logged.loggedSlots - before.loggedSlots == 1024 &&
                logged.oldTargetChecks - before.oldTargetChecks == 1024,
                "every simultaneous context actually logs one promoted owned field before its first allocation");
            if (!Epoch(engine, {owner}))
            {
                return;
            }
            LxrRcMetrics drained = engine.GetMetrics();
            Check(drained.incrementEntries - before.incrementEntries == 1024 &&
                drained.decrementEntries - before.decrementEntries -
                    (drained.rootDecrements - before.rootDecrements) == 1024 &&
                drained.bufferFlushes - before.bufferFlushes == 2048,
                "all1024 one-entry increment and decrement buffers are published and consumed without loss");
            for (size_t index = 0; index < mutators.size(); index++)
            {
                Check(fields[index] == replacement, "all partial producer buffers preserve actual reference fields");
            }
        }
    }

    void ValidateStorageDiagnostics()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator;
        int identity;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Check(engine.EnableStorageDiagnostics(Timeout) == LxrRcResult::Success &&
            engine.EnableStorageDiagnostics(Timeout) == LxrRcResult::InvalidPhase,
            "storage accounting enables once with a quiescent inactive trace");
        uintptr_t address = 1;
        size_t bytes = 1;
        Check(engine.GetStorageRange(SIZE_MAX, &address, &bytes) == LxrRcResult::InvalidArgument &&
            address == 0 && bytes == 0, "invalid component ranges do not expose stale descriptors");
        Check(engine.GetStorageRange(0, &address, &bytes) == LxrRcResult::Success &&
            address != 0 && bytes >= engine.GetStorageMetric(7),
            "extent coverage includes every actually committed page");
        for (size_t component = 17; component < 17 + LxrSideMetadataLayout::SpecCount; component++)
        {
            Check(engine.GetStorageRange(component, &address, &bytes) == LxrRcResult::Success &&
                address % 4096 == 0 && bytes != 0 && bytes % 4096 == 0,
                "side metadata coverage is page-aligned and includes the whole reservation");
        }
        Type type(24, 0);
        Object* first = Allocate(engine, mutator, type);
        Object* second = Allocate(engine, mutator, type);
        if (first == nullptr || second == nullptr)
        {
            return;
        }
        Epoch(engine, {first, second});
        Check(engine.GetStorageMetric(0) >= 3 && engine.GetStorageMetric(2) >= 16 &&
            engine.GetStorageMetric(3) == 2 * sizeof(uintptr_t),
            "actual packet allocation and copied payload accounting matches the two copied roots");
        Check(engine.GetStorageMetric(23) == 2 && engine.GetStorageMetric(24) == 1 &&
            engine.GetStorageMetric(25) == 2 * 1024 * sizeof(uintptr_t),
            "captured root and active-context occupancy are separate from capacity");
        Epoch(engine, {});
        Check(engine.GetLiveBytes() == 0 && engine.GetStorageMetric(3) == 4 * sizeof(uintptr_t),
            "next-epoch root snapshots remain independent and accounted");
    }

    void ValidateObjectPlanes()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator;
        int identity;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Type type(65536, 0);
        Object* first = Allocate(engine, mutator, type);
        Object* second = Allocate(engine, mutator, type);
        if (first == nullptr || second == nullptr)
        {
            return;
        }
        uintptr_t start = reinterpret_cast<uintptr_t>(first) - sizeof(ObjHeader);
        Check(sizeof(ObjHeader) == 8 && start % LxrLargeObjectSpace::PageBytes == 0,
            "LOS supplemental state is keyed by the exact page-aligned Object minus header start");
        Check(engine.GetObjectExtent(reinterpret_cast<Object*>(reinterpret_cast<uintptr_t>(first) + 8)) == 0 &&
            engine.GetObjectExtent(reinterpret_cast<Object*>(reinterpret_cast<uintptr_t>(first) + 4096)) == 0,
            "page storage cannot turn an interior pointer into an object identity");
        for (size_t offset : {sizeof(ObjHeader), size_t{2 * 4096 + 17}, size_t{65535}})
        {
            Object* found = nullptr;
            Check(engine.FindContainingObject(start + offset, &found) == LxrRcResult::Success && found == first,
                "span ownership resolves interior roots beyond the first page without rounding them into starts");
        }
        Object* found = nullptr;
        Check(engine.FindContainingObject(start, &found) == LxrRcResult::InvalidArgument && found == nullptr,
            "an object header is not an interior managed root");
        Epoch(engine, {first, second});
        Check(engine.GetObjectExtent(first) == 65536 && engine.GetObjectExtent(second) == 65536,
            "separate LOS allocations retain separate supplemental state");
        Epoch(engine, {});
        Check(engine.GetObjectExtent(first) == 0 && engine.GetObjectExtent(second) == 0,
            "LOS extent retirement happens before independent payload release");
    }

    void ValidateAuxiliaryRetirement()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator;
        int identity;
        if (!Initialize(engine, 8, &mutator, &identity) ||
            !Check(engine.EnableStorageDiagnostics(Timeout) == LxrRcResult::Success,
                "retention diagnostics enable without retaining an object"))
        {
            return;
        }
        Type arrayType(65536, 4097);
        Type nodeType(24, 0);
        Object* array = Allocate(engine, mutator, arrayType);
        if (array == nullptr)
        {
            return;
        }
        Object** fields = reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(array) + sizeof(void*));
        for (size_t index = 0; index < 4097; index++)
        {
            Object* child = Allocate(engine, mutator, nodeType);
            if (child == nullptr || !Check(
                engine.LogBeforeWrite(mutator, fields + index, fields[index]) == LxrRcResult::Success,
                "retained reference-rich input uses the real pre-store contract"))
            {
                return;
            }
            fields[index] = child;
        }
        std::vector<Object*> values{array};
        Roots roots{&values};
        for (size_t cycle = 0; cycle < 3; cycle++)
        {
            Check(engine.CollectFull(Capture, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
                "a real full collection and all native readers finish");
            Check(engine.GetObjectExtent(array) == 65536 &&
                engine.GetLiveBytes() == 65536 + 4097 * 24,
                "recommit preserves reference-rich old objects");
            Check(engine.GetStorageMetric(1) == 0 && engine.GetStorageMetric(5) == 0 &&
                engine.GetStorageMetric(8) == 0 && engine.GetStorageMetric(9) == 0,
                "completed packets, scan descriptors and trace planes do not retain historical high water");
        }
        Check(engine.GetStorageMetric(0) != 0 && engine.GetStorageMetric(2) != 0,
            "zero retained bytes is backed by actual allocations and high-water evidence");
        Check(engine.CollectFull(nullptr, nullptr, Timeout) == LxrRcResult::Success &&
            engine.Drain(Timeout) == LxrRcResult::Success && engine.GetLiveBytes() == 0,
            "reference-rich reclamation still precedes auxiliary retirement");
    }

    void ValidateRootBatching()
    {
        for (size_t count : {size_t{1}, size_t{2}, size_t{1023}, size_t{1024}, size_t{1025}})
        {
            LxrReferenceCounting engine;
            LxrRcConfiguration configuration{
                16 * ImmixBlockGeometry::BlockBytes, 8 * LxrLargeObjectSpace::MinimumObjectBytes,
                16, 2048, 4, 8,
            };
            LxrPauseRuntimeAdapter runtime{Suspend, Resume, nullptr, false};
            LxrRcMutator* mutator = nullptr;
            int identity;
            if (!Check(engine.Initialize(configuration, runtime) == LxrRcResult::Success &&
                    engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success &&
                    engine.EnableStorageDiagnostics(Timeout) == LxrRcResult::Success,
                    "root batching fixture owns its exact source and accounting"))
            {
                return;
            }
            Type type(24, 0);
            std::vector<Object*> values;
            for (size_t index = 0; index < count; index++)
            {
                Object* object = Allocate(engine, mutator, type);
                if (object == nullptr)
                {
                    return;
                }
                values.push_back(object);
            }
            Roots roots{&values};
            Check(engine.CollectFull(Capture, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "batched full root tracing completes");
            Check(engine.GetStorageMetric(3) == 2 * count * sizeof(uintptr_t),
                "each root is copied once for RC and once for the batched trace seed (P25 F1: no copy for an increment trace)");
            Check(engine.GetLiveBytes() == count * 24 && engine.GetStorageMetric(1) == 0,
                "exact root batching preserves all payload and retires its packets");
        }
    }

    void ValidateTraceBatching()
    {
        for (size_t count : {size_t{1}, size_t{1023}, size_t{1024}, size_t{1025}})
        {
            for (size_t mode = 0; mode < 3; mode++)
            {
                LxrReferenceCounting engine;
                LxrRcConfiguration configuration{
                    64 * ImmixBlockGeometry::BlockBytes, 8 * LxrLargeObjectSpace::MinimumObjectBytes,
                    4, 2048, 4, 8,
                };
                configuration.evacuateNursery = mode != 0;
                configuration.evacuateMature = mode == 2;
                MovingRoots roots;
                LxrRcObjectModel model{};
                model.context = &roots;
                model.finishRelocation = RelocateMoving;
                LxrPauseRuntimeAdapter runtime{Suspend, Resume, nullptr, false};
                LxrRcMutator* mutator = nullptr;
                int identity;
                if (!Check(engine.Initialize(configuration, runtime, &model) == LxrRcResult::Success &&
                        engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success &&
                        engine.EnableStorageDiagnostics(Timeout) == LxrRcResult::Success,
                        "trace batch fixture owns four workers and complete storage accounting"))
                {
                    return;
                }
                Type parentType(88, 8);
                Type leafType(24, 0);
                for (size_t index = 0; index < count; index++)
                {
                    Object* parent = Allocate(engine, mutator, parentType);
                    if (parent == nullptr)
                    {
                        return;
                    }
                    for (size_t field = 0; field < 8; field++)
                    {
                        Object* child = Allocate(engine, mutator, leafType);
                        if (child == nullptr || !Store(engine, mutator, Field(parent) + field, child))
                        {
                            return;
                        }
                        *reinterpret_cast<uintptr_t*>(reinterpret_cast<uint8_t*>(child) + sizeof(void*)) =
                            index * 8 + field + 1;
                    }
                    roots.objects.push_back(parent);
                }
                if (!Check(engine.CollectFull(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                        engine.Drain(Timeout) == LxrRcResult::Success,
                        "cross-object full and partial batches complete before worker and epoch handoff"))
                {
                    return;
                }
                uint64_t packets = engine.GetStorageMetric(0);
                size_t packetBound = 32 + 8 * ((count * 9 + 1023) / 1024);
                std::printf("trace_batch parents=%zu mode=%zu packets=%llu bound=%zu targets=%llu flushes=%llu "
                    "combined_peak=%llu pending_peak=%llu\n", count, mode,
                    static_cast<unsigned long long>(packets), packetBound,
                    static_cast<unsigned long long>(engine.GetStorageMetric(53)),
                    static_cast<unsigned long long>(engine.GetStorageMetric(54)),
                    static_cast<unsigned long long>(engine.GetStorageMetric(2)),
                    static_cast<unsigned long long>(engine.GetStorageMetric(56)));
                Check(packets <= packetBound,
                    "actual trace packet count is bounded by batches, not individual objects");
                Check(engine.GetStorageMetric(53) >= count * 8,
                    "small-object descendants of a traced parent use the trace target accumulator (P26: a flush happens only when the batch fills)");
                Check(engine.GetStorageMetric(55) == 0 && engine.GetStorageMetric(56) >=
                        LxrReferenceCounting::FullTraceBatchCapacity * sizeof(uintptr_t) &&
                        engine.GetStorageMetric(2) >= engine.GetStorageMetric(56) &&
                        engine.GetStorageMetric(2) <= TraceBatchFixtureMaximumWorkBytes &&
                        engine.GetStorageMetric(6) <= TraceBatchFixtureMaximumScanBytes,
                    "pending stack capacity is included in packet ownership and fully released");
                for (size_t index = 0; index < count; index++)
                {
                    for (size_t field = 0; field < 8; field++)
                    {
                        Object* child = Field(roots.objects[index])[field];
                        Check(engine.GetObjectExtent(child) == 24 && engine.IsMarked(child) &&
                                *reinterpret_cast<uintptr_t*>(reinterpret_cast<uint8_t*>(child) + sizeof(void*)) ==
                                    index * 8 + field + 1,
                            "published trace targets retain each exact moved child payload");
                    }
                }
                Check(engine.ValidateTraceBatchForTest(roots.objects[0], count == 1 ? 1 :
                        LxrReferenceCounting::FullTraceBatchCapacity + 1) == LxrRcResult::Success,
                    "unpublished partial and full buffers retain explicit trace completion ownership");
                Check(engine.GetStorageMetric(1) == 0 && engine.GetStorageMetric(55) == 0,
                    "final partial publication leaves no hidden pending storage");
                if (!Check(engine.CollectFull(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                        engine.Drain(Timeout) == LxrRcResult::Success,
                        "the next full epoch repairs and retires published moving readers"))
                {
                    return;
                }
                Check(engine.GetLiveBytes() == count * (88 + 8 * 24),
                    "batching conserves live object extents across the next epoch");
                Check(engine.DetachMutator(mutator) == LxrRcResult::Success,
                    "trace batch fixture mutator detaches after all work drains");
            }
        }
    }

    void ValidateTraceBatchSlices()
    {
        constexpr size_t Fields = LxrReferenceCounting::FullTraceBatchCapacity + 1;
        for (size_t mode = 0; mode < 3; mode++)
        {
            LxrReferenceCounting engine;
            LxrRcConfiguration configuration{
                64 * ImmixBlockGeometry::BlockBytes, 8 * LxrLargeObjectSpace::MinimumObjectBytes,
                4, 2048, 4, 8,
            };
            configuration.evacuateNursery = mode != 0;
            configuration.evacuateMature = mode == 2;
            MovingRoots roots;
            LxrRcObjectModel model{};
            model.context = &roots;
            model.finishRelocation = RelocateMoving;
            LxrPauseRuntimeAdapter runtime{Suspend, Resume, nullptr, false};
            LxrRcMutator* mutator = nullptr;
            int identity;
            if (!Check(engine.Initialize(configuration, runtime, &model) == LxrRcResult::Success &&
                    engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success &&
                    engine.EnableStorageDiagnostics(Timeout) == LxrRcResult::Success,
                    "sliced full trace fixture owns its descriptor and target storage"))
            {
                return;
            }
            Type arrayType(static_cast<uint32_t>(24 + Fields * sizeof(uintptr_t)), Fields);
            Type leafType(24, 0);
            Object* array = Allocate(engine, mutator, arrayType);
            if (array == nullptr)
            {
                return;
            }
            roots.objects.push_back(array);
            for (size_t field = 0; field < Fields; field++)
            {
                Object* child = Allocate(engine, mutator, leafType);
                if (child == nullptr || !Store(engine, mutator, Field(array, field), child))
                {
                    return;
                }
            }
            if (!Check(engine.CollectFull(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                    engine.Drain(Timeout) == LxrRcResult::Success,
                    "full trace publishes the last partial descriptor slice before completion"))
            {
                return;
            }
            Check(engine.GetTraceMetrics().splitPackets >= Fields / LxrReferenceCounting::BufferCapacity &&
                    engine.GetStorageMetric(0) < 64 && engine.GetStorageMetric(1) == 0 &&
                    engine.GetStorageMetric(5) == 0 && engine.GetStorageMetric(55) == 0 &&
                    engine.GetStorageMetric(2) <= TraceBatchFixtureMaximumWorkBytes &&
                    engine.GetStorageMetric(6) <= TraceBatchFixtureMaximumScanBytes,
                "real sliced scanners use bounded owned packets and release every descriptor and batch");
            for (size_t field = 0; field < Fields; field++)
            {
                Object* child = *Field(roots.objects[0], field);
                Check(engine.GetObjectExtent(child) == 24 && engine.IsMarked(child),
                    "every field including the final partial slice retains its exact live target");
            }
            Check(engine.DetachMutator(mutator) == LxrRcResult::Success,
                "sliced trace mutator detaches after publication and draining");
        }
    }

    int BirthStudy()
    {
        std::printf("sample,operations,allocated_bytes,elapsed_ns\n");
        for (size_t sample = 0; sample < 7; sample++)
        {
            LxrReferenceCounting engine;
            LxrRcMutator* mutator;
            int identity;
            if (!Initialize(engine, 8, &mutator, &identity))
            {
                return 1;
            }
            Type type(24, 0);
            if (Allocate(engine, mutator, type) == nullptr)
            {
                return 1;
            }
            uintptr_t cursor;
            uintptr_t limit;
            if (!Check(engine.GetAllocationSpan(mutator, &cursor, &limit) == LxrRcResult::Success,
                "birth study exports its initialized allocation span"))
            {
                Check(false, "birth study span refill failed");
                return 1;
            }
            constexpr size_t Operations = 20000;
            uint64_t before = engine.GetMetrics().allocatedBytes;
            std::chrono::steady_clock::time_point start = std::chrono::steady_clock::now();
            for (size_t index = 0; index < Operations; index++)
            {
                if (limit - cursor < 24)
                {
                    if (engine.RetireAllocationSpan(mutator, cursor) != LxrRcResult::Success ||
                        Allocate(engine, mutator, type) == nullptr ||
                        engine.GetAllocationSpan(mutator, &cursor, &limit) != LxrRcResult::Success)
                    {
                        Check(false, "birth study fast notification failed");
                        return 1;
                    }
                }
                Object* object = reinterpret_cast<Object*>(cursor + sizeof(ObjHeader));
                object->RawSetMethodTable(type.table);
                cursor += 24;
            }
            uint64_t elapsed = static_cast<uint64_t>(std::chrono::duration_cast<std::chrono::nanoseconds>(
                std::chrono::steady_clock::now() - start).count());
            if (!Check(engine.RetireAllocationSpan(mutator, cursor) == LxrRcResult::Success,
                "birth study retires its remaining span"))
            {
                return 1;
            }
            std::printf("%zu,%zu,%llu,%llu\n", sample, Operations,
                static_cast<unsigned long long>(engine.GetMetrics().allocatedBytes - before),
                static_cast<unsigned long long>(elapsed));
        }
        return 0;
    }

    void ValidateFastSpan()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator;
        int identity;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Type type(24, 0);
        Object* slow = nullptr;
        Check(engine.Allocate(mutator, 24, &slow) == LxrRcResult::Success, "span refill owns its first extent");
        uintptr_t cursor;
        uintptr_t limit;
        Check(engine.GetAllocationSpan(mutator, &cursor, &limit) == LxrRcResult::Success &&
            cursor != 0 && limit > cursor, "real Immix hole is exported without allocating a second arena");
        slow->RawSetMethodTable(type.table);
        std::vector<Object*> roots{slow};
        for (size_t index = 0; index < 512; index++)
        {
            Object* object = reinterpret_cast<Object*>(cursor + sizeof(ObjHeader));
            Check(engine.GetObjectExtent(object) == 0 && object->GetHeader()->GetBits() == 0 &&
                !engine.IsObjectStart(object), "unconsumed span has no registered object and a zero header");
            object->RawSetMethodTable(type.table);
            engine.RecordHelperObjectForTest(object, 24);
            cursor += 24;
            roots.push_back(object);
        }
        Check(engine.RetireAllocationSpan(mutator, cursor) == LxrRcResult::Success,
            "the used point of the buffer is accepted");
        Check(engine.GetMetrics().allocations == 1,
            "only the object of the slow path is a call to the engine; the objects of the buffer are not");
        for (Object* object : roots)
        {
            Check(!engine.IsObjectStart(object), "a young object has no ValidObject bit");
        }
        Epoch(engine, roots);
        Check(engine.GetMetrics().allocatedBytes == 513 * 24, "the span counts the exact used bytes");
        auto count = [&](Object* object)
        {
            uintptr_t value = UINTPTR_MAX;
            engine.LoadCount(object, &value);
            return value;
        };
        bool promoted = true;
        for (Object* object : roots)
        {
            promoted = promoted && engine.IsObjectStart(object) && count(object) == 1;
        }
        Check(promoted, "a survivor gets its ValidObject bit and its count at the pause");
        Epoch(engine, {});
        bool dead = true;
        for (Object* object : roots)
        {
            dead = dead && !engine.IsObjectStart(object) && count(object) == 0;
        }
        Check(dead, "an object that lost its last root dies and loses its bit");
    }

    void ValidateGrowth()
    {
        LxrReferenceCounting engine;
        LxrRcConfiguration configuration{
            2 * ImmixBlockGeometry::BlockBytes, 4 * LxrLargeObjectSpace::MinimumObjectBytes,
            4, 1024, 4, 8, 2 * 1024 * 1024, 4 * 1024 * 1024, 4 * 1024 * 1024,
        };
        LxrPauseRuntimeAdapter runtime{Suspend, Resume, nullptr, false};
        LxrRcMutator* mutator = nullptr;
        int identity;
        if (!Check(engine.Initialize(configuration, runtime) == LxrRcResult::Success,
                "sparse growth initializes only the admitted capacity") ||
            !Check(engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success, "growth mutator attaches"))
        {
            return;
        }
        size_t initialNative = engine.GetNativePlaneCommittedBytes();
        size_t initial = engine.GetAdmittedBytes();
        uintptr_t start = engine.GetReservationStart();
        uintptr_t descriptor = engine.GetFieldMetadataBase();
        std::vector<Object*> roots;
        Roots capture{&roots};
        for (size_t bytes : {size_t{8192}, size_t{65536}})
        {
            Type type(static_cast<uint32_t>(bytes), 0);
            for (size_t index = 0; index < (bytes == 8192 ? 100 : 24); index++)
            {
                Object* object = nullptr;
                LxrRcResult result = engine.Allocate(mutator, bytes, &object);
                if (result == LxrRcResult::AllocationFailure)
                {
                    Check(engine.Grow(bytes >= 32768, bytes, Capture, &capture, Timeout) == LxrRcResult::Success,
                        "allocation exhaustion commits real additional capacity");
                    result = engine.Allocate(mutator, bytes, &object);
                }
                if (!Check(result == LxrRcResult::Success, "growth makes the requested extent allocatable"))
                {
                    return;
                }
                object->RawSetMethodTable(type.table);
                roots.push_back(object);
            }
            Epoch(engine, roots);
            // Keep the MethodTable alive through each phase; dead payload is never parsed afterward.
            roots.clear();
            Epoch(engine, {});
        }
        Check(engine.GetAdmittedBytes() > initial && engine.GetGrowthCount() > 0,
            "capacity growth is measured rather than a larger eager arena");
        Check(engine.GetNativePlaneCommittedBytes() > initialNative &&
            engine.GetReservationStart() == start && engine.GetFieldMetadataBase() == descriptor,
            "committed native planes grow without changing any published address");
        Check(engine.Grow(false, configuration.hardLimitBytes, Capture, &capture, Timeout) ==
                LxrRcResult::AllocationFailure && engine.GetFailure() == LxrRcResult::Success,
            "hard-limit failure does not poison existing owned storage");
        Check(engine.GetLiveBytes() == 0, "grown arenas retain ordinary reclamation");
    }

    void ValidatePolicy()
    {
        LxrCollectionPolicy reserve;
        for (size_t index = 0; index < 600; index++)
        {
            reserve.BeginPause(1);
            reserve.ReleasePause();
        }
        Check(reserve.BeginPause(3) && reserve.RecordCopiedPromotion(2) && reserve.ReleasePause() &&
                reserve.SurvivalRatio() == 0.5, "reserve distinguishing predictor is exactly one half");
        Check(reserve.CollectionReservedPages(size_t{64} << 20, 0) == 64,
            "literal shipped reserve adds 64 rather than a unit-corrected 16384 pages");
        Check(reserve.CollectionReservedPages((size_t{1} << 20) - 1, 7) == 7 &&
                reserve.CollectionReservedPages(size_t{5} << 20, 7) == 11,
            "literal reserve floors MiB, truncates survival before doubling, then adds real headroom pages");
        LxrPolicyPressure reserved{false, true, true, 0, 0, 0, true, 0, 64, 128, 64};
        Check(reserve.Trigger(reserved) == LxrCollectionCause::HeapFull,
            "actual admitted pressure plus reference reserve precedes trace drainage");
        reserved.collectionReservedPages = 63;
        Check(reserve.Trigger(reserved) == LxrCollectionCause::FinalMark,
            "a reserve below the admission boundary does not impersonate space exhaustion");
        {
            // The to-space is taken from free Immix pages. Free large-object pages cannot hold it.
            LxrPolicyPressure copyPressure{false, false, false, 0, 0, 0, true, 0, 10, 1000, 0};
            copyPressure.copyReservePages = 100;
            copyPressure.immixFreePages = 100;
            Check(reserve.Trigger(copyPressure) == LxrCollectionCause::HeapFull,
                "a to-space reserve that does not fit in the free Immix pages starts a collection while other pages are free");
            copyPressure.immixFreePages = 101;
            Check(reserve.Trigger(copyPressure) == LxrCollectionCause::None,
                "a to-space reserve below the free Immix pages does not start a collection");
        }
        {
            // Port-specific byte-sized to-space reserve (not the Java LXR formula).
            constexpr size_t Page = 4096;
            LxrCollectionPolicy copying;
            Check(copying.ReserveSurvivalRatio() == 1.0 &&
                    copying.CollectionReservedPagesForCopy(size_t{64} << 20, Page) == 2 * (size_t{64} << 20) / Page,
                "before any measurement every young byte is assumed to survive, in real pages");
            Check(copying.BeginPause(size_t{100} << 20) && copying.RecordCopiedPromotion(0) &&
                    copying.RecordSurvivors(size_t{25} << 20) && copying.ReleasePause() &&
                    copying.MeasuredSurvivalRatio() == 0.25 &&
                    std::abs(copying.SurvivalRatio() - 0.0025) < 1e-12,
                "in-place survivors are measured when no byte was copied, and the first sample replaces the prior");
            Check(copying.CollectionReservedPagesForCopy(size_t{64} << 20, Page) == size_t{8192} &&
                    copying.CollectionReservedPagesForCopy(1, Page) == 1 &&
                    copying.CollectionReservedPagesForCopy(0, Page) == 0,
                "reserve is young bytes times ratio times two, rounded up to whole pages");
            Check(copying.BeginPause(size_t{100} << 20) && copying.RecordSurvivors(size_t{50} << 20) &&
                    copying.ReleasePause() && std::abs(copying.MeasuredSurvivalRatio() - 0.4375) < 1e-12,
                "a rising measurement is weighted three to one");
            Check(copying.BeginPause(size_t{100} << 20) && copying.ReleasePause() &&
                    std::abs(copying.MeasuredSurvivalRatio() - 0.328125) < 1e-12,
                "a falling measurement is weighted one to three");
            Check(!copying.RecordSurvivors(1), "survivors outside a pause fail explicitly");
            for (size_t index = 0; index < 200; index++)
            {
                copying.BeginPause(size_t{100} << 20);
                copying.ReleasePause();
            }
            Check(copying.MeasuredSurvivalRatio() < 1e-9 && copying.ReserveSurvivalRatio() == 0.05 &&
                    copying.CollectionReservedPagesForCopy(size_t{20} << 20, Page) == 512,
                "a quiet phase keeps a minimum reserve");
            {
                // Young bytes in recycled blocks are promoted in place and never copied: they neither size the
                // measured ratio nor need a to-space.
                LxrCollectionPolicy evacuable;
                Check(evacuable.BeginPause(size_t{100} << 20, size_t{20} << 20) &&
                        evacuable.RecordSurvivors(size_t{10} << 20) && evacuable.ReleasePause() &&
                        evacuable.MeasuredSurvivalRatio() == 0.5,
                    "the measured ratio is taken against the evacuable young bytes");
                Check(evacuable.BeginPause(size_t{100} << 20, 0) && evacuable.ReleasePause() &&
                        evacuable.MeasuredSurvivalRatio() == 0.5,
                    "a pause without evacuable bytes leaves the measured ratio unchanged");
                Check(evacuable.CollectionReservedPagesForCopy(0, Page) == 0,
                    "no clean nursery bytes need no to-space, whatever the ratio");
            }
        }
        LxrCollectionPolicy policy;
        Check(policy.BeginPause(0) && policy.RecordCopiedPromotion(100) && policy.ReleasePause() &&
            policy.SurvivalRatio() == 0.01, "zero allocation preserves prediction and discards copied volume");
        Check(!policy.ReleasePause() && !policy.RecordCopiedPromotion(1),
            "predictor events outside a pause fail explicitly");
        Check(policy.BeginPause(100) && !policy.BeginPause(100) &&
            policy.RecordCopiedPromotion(200) && policy.ReleasePause() &&
            policy.SurvivalRatio() == 0.7525, "default rising predictor clamps survival before weighting");
        Check(policy.BeginPause(100) && policy.ReleasePause() &&
            std::abs(policy.SurvivalRatio() - 0.188125) < 1e-12, "default falling predictor uses 3:1 current weight");
        LxrCollectionPolicy weighted;
        LxrPolicyConfiguration configuration;
        configuration.weighted = true;
        weighted.Configure(configuration);
        Check(weighted.BeginPause(100) && weighted.RecordCopiedPromotion(100) && weighted.ReleasePause() &&
            weighted.SurvivalRatio() == 0.7525, "weighted rising variant is 3:1");
        Check(weighted.BeginPause(100) && weighted.ReleasePause() &&
            std::abs(weighted.SurvivalRatio() - 0.564375) < 1e-12, "weighted falling variant is 1:3");
        LxrCollectionPolicy harmonic;
        configuration.weighted = false;
        configuration.harmonic = true;
        harmonic.Configure(configuration);
        Check(harmonic.BeginPause(100) && harmonic.RecordCopiedPromotion(100) && harmonic.ReleasePause() &&
            std::abs(harmonic.SurvivalRatio() - 2.0 * 0.01 / 1.01) < 1e-12,
            "harmonic rising variant matches the coupled oracle");
        double prior = harmonic.SurvivalRatio();
        Check(harmonic.BeginPause(100) && harmonic.ReleasePause() &&
            std::abs(harmonic.SurvivalRatio() - prior) < 1e-12, "harmonic falling zero-survival retains prior");
        LxrCollectionPolicy zero;
        for (size_t index = 0; index < 600; index++)
        {
            zero.BeginPause(1);
            zero.ReleasePause();
        }
        Check(zero.SurvivalRatio() == 0.0, "nonmoving copied-volume prediction reaches exact zero");
        zero.Configure(configuration);
        Check(zero.BeginPause(1) && zero.ReleasePause() && zero.SurvivalRatio() == 1.0,
            "harmonic zero-over-zero preserves Rust f64::min NaN semantics");
        Check(!policy.FinishLazySweep(LxrPauseKind::Full, 1000, 200, false, true, false) &&
            policy.MatureLivePages() == 0.0, "unfinished lazy sweep cannot feed mature-live prediction");
        Check(policy.FinishLazySweep(LxrPauseKind::RefCount, 1000, 200, false, true, true) &&
            policy.MatureLivePages() == 0.0, "RefCount does not update mature-live prediction");
        Check(policy.FinishLazySweep(LxrPauseKind::Full, 1000, 200, false, true, true) &&
            policy.MatureLivePages() == 150.0, "Full updates mature-live prediction at 3:1");
        Check(policy.FinishLazySweep(LxrPauseKind::InitialMark, 1000, 900, true, true, true) &&
            policy.MatureLivePages() == 150.0 && !policy.CycleHint(),
            "InitialMark neither updates mature-live nor restarts an in-flight cycle");
        Check(policy.FinishLazySweep(LxrPauseKind::FinalMark, 1000, 400, false, true, true) &&
            policy.MatureLivePages() == 337.5, "FinalMark updates mature-live exactly once");
        Check(policy.Select(true, true, true, true, false) == LxrPauseKind::FinalMark &&
            policy.Select(true, false, true, false, true) == LxrPauseKind::FinalMark &&
            policy.Select(true, false, false, true, true) == LxrPauseKind::FinalMark,
            "trace drainage and forced requests finish in-flight marking before full selection");
        Check(policy.Select(false, false, true, false, true) == LxrPauseKind::Full &&
            policy.Select(false, false, false, true, true) == LxrPauseKind::Full,
            "emergency and user requests select a truthful full collection");
        Check(policy.FinishLazySweep(LxrPauseKind::RefCount, 1000, 800, false, true, true) &&
            policy.CycleHint() && !policy.EmergencyHint() &&
            policy.Select(false, false, false, false, true) == LxrPauseKind::InitialMark &&
            policy.Select(false, false, false, false, false) == LxrPauseKind::Full,
            "cycle hint precedes RC and falls back to full without concurrent marking");
        Check(policy.Select(true, false, false, false, true) == LxrPauseKind::RefCount,
            "cycle hint does not restart an unfinished trace");
        Check(policy.FinishLazySweep(LxrPauseKind::RefCount, 1000, 950, false, true, true) &&
            policy.EmergencyHint() && policy.Select(true, false, false, false, true) == LxrPauseKind::FinalMark,
            "emergency hint precedes cycle and RC");
        Check(policy.FinishLazySweep(LxrPauseKind::RefCount, 3000000, 2730000, false, true, true) &&
            !policy.EmergencyHint(), "emergency headroom is capped at the oracle's one GiB");
        LxrPolicyPressure pressure{true, true, true, 0, 0, 0, true, 0};
        Check(policy.Trigger(pressure) == LxrCollectionCause::HeapFull, "heap-full trigger precedes trace drainage");
        pressure.heapFull = false;
        Check(policy.Trigger(pressure) == LxrCollectionCause::FinalMark, "trace drainage precedes allocation caps");
        LxrCollectionPolicy triggers;
        configuration = {};
        configuration.maxSurvivalMB = SIZE_MAX;
        configuration.incrementsLimit = 4;
        configuration.nurseryBlocks = 2;
        configuration.youngLimitBytes = 3 * 1024 * 1024;
        triggers.Configure(configuration);
        pressure = {false, false, false, 3 * 1024 * 1024, 4, 2, true, 0};
        Check(triggers.Trigger(pressure) == LxrCollectionCause::Increments, "increment limit precedes both young caps");
        pressure.increments = 0;
        Check(triggers.Trigger(pressure) == LxrCollectionCause::FixedNursery, "clean nursery cap precedes total allocation cap");
        pressure.cleanNurseryBlocks = 0;
        Check(triggers.Trigger(pressure) == LxrCollectionCause::YoungAllocation, "total young allocation cap is last");
        configuration.maxSurvivalMB = 1;
        triggers.Configure(configuration);
        pressure.youngBytes = 128 * 1024 * 1024;
        pressure.contiguous = false;
        Check(triggers.Trigger(pressure) == LxrCollectionCause::Survival, "survival cap precedes space and increment limits");
        configuration.maxSurvivalMB = SIZE_MAX;
        triggers.Configure(configuration);
        Check(triggers.Trigger(pressure) == LxrCollectionCause::ImmixSpaceFull,
            "noncontiguous Immix availability precedes increment limits");
        pressure.contiguous = true;
        pressure.youngBytes = 0;
        Check(triggers.Trigger(pressure) == LxrCollectionCause::None, "contiguous heap ignores noncontiguous availability");
    }

    void ValidateGrowthFailure()
    {
        // Three object planes (S7 removed the two trace tables), four payload streams, fifteen metadata planes, then
        // the managed heap.
        constexpr int64_t CommitCalls = 23;
        for (int64_t after = 0; after <= CommitCalls; after++)
        {
            LxrReferenceCounting engine;
            LxrRcConfiguration configuration{
                2 * ImmixBlockGeometry::BlockBytes, 4 * LxrLargeObjectSpace::MinimumObjectBytes,
                4, 1024, 4, 8, 1024 * 1024, 1024 * 1024, 2 * 1024 * 1024,
            };
            LxrPauseRuntimeAdapter runtime{Suspend, Resume, nullptr, false};
            LxrRcMutator* mutator = nullptr;
            int identity;
            if (!Check(engine.Initialize(configuration, runtime) == LxrRcResult::Success,
                    "commit failure fixture initializes") ||
                !Check(engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                    "commit failure mutator attaches"))
            {
                return;
            }
            Type type(24, 0);
            Object* object = nullptr;
            Check(engine.Allocate(mutator, 24, &object) == LxrRcResult::Success,
                "preexisting object allocates before growth failure");
            object->RawSetMethodTable(type.table);
            std::vector<Object*> values{object};
            Roots roots{&values};
            size_t capacity = engine.GetAdmittedBytes();
            LxrTestFailCommitAfter(after);
            LxrRcResult result = engine.Grow(false, 32768, Capture, &roots, Timeout);
            LxrTestFailCommitAfter(-1);
            std::printf("growth_failure_after=%lld result=%u admitted_before=%zu admitted_after=%zu\n",
                static_cast<long long>(after), static_cast<unsigned>(result), capacity, engine.GetAdmittedBytes());
            Check((after == CommitCalls
                    ? result == LxrRcResult::Success && engine.GetAdmittedBytes() > capacity
                    : result == LxrRcResult::OutOfMemory && engine.GetAdmittedBytes() == capacity) &&
                engine.GetFailure() == LxrRcResult::Success && engine.GetObjectExtent(object) == 24,
                "partial plane, stream, metadata or heap commit failure does not publish capacity or poison old objects");
            Check((after == CommitCalls ||
                    engine.Grow(false, 32768, Capture, &roots, Timeout) == LxrRcResult::Success) &&
                engine.GetAdmittedBytes() > capacity,
                "failed admission can recover without reallocating callback storage");
            Epoch(engine, values);
            Check(engine.GetLiveBytes() == 24, "post-failure roots and reclamation stay valid");
        }
    }

    void ValidateCoalescedGrowth()
    {
        LxrReferenceCounting engine;
        LxrRcConfiguration configuration{
            2 * ImmixBlockGeometry::BlockBytes, 1024 * 1024, 4, 1024, 4, 8,
            2 * ImmixBlockGeometry::BlockBytes, 2 * 1024 * 1024, 3 * 1024 * 1024,
        };
        LxrPauseRuntimeAdapter runtime{Suspend, Resume, nullptr, false};
        // The managed ceiling need not fill both reservations.
        configuration.hardLimitBytes = configuration.immixReservationBytes + configuration.largeObjectReservationBytes;
        LxrRcMutator* mutator = nullptr;
        int identity;
        if (!Check(engine.Initialize(configuration, runtime) == LxrRcResult::Success,
                "coalesced LOS fixture initializes") ||
            !Check(engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success, "coalesced mutator attaches"))
        {
            return;
        }
        Type type(1536 * 1024, 0);
        Object* object = nullptr;
        Check(engine.Allocate(mutator, type.table->GetBaseSize(), &object) == LxrRcResult::AllocationFailure,
            "single object does not fit initial LOS");
        std::vector<Object*> values;
        Roots roots{&values};
        Check(engine.Grow(true, type.table->GetBaseSize(), Capture, &roots, Timeout) == LxrRcResult::Success,
            "only missing tail pages are required for growth");
        Check(engine.Allocate(mutator, type.table->GetBaseSize(), &object) == LxrRcResult::Success,
            "coalesced initial and added tail admit the entire extent");
        object->RawSetMethodTable(type.table);
        values.push_back(object);
        Epoch(engine, values);
        Check(engine.GetLiveBytes() == type.table->GetBaseSize(), "large coalesced survivor remains live");
    }

    struct PolicyRuntime
    {
        LxrReferenceCounting* engine;
        LxrCollectionPolicy policy;
        LxrPauseKind pause = LxrPauseKind::RefCount;
        std::atomic<bool> resumed{true};
        std::atomic<size_t> feeds{0};
        size_t totalPages;
    };

    void PolicySuspend(void* context)
    {
        PolicyRuntime* runtime = static_cast<PolicyRuntime*>(context);
        runtime->resumed.store(false);
        Check(runtime->policy.BeginPause(runtime->engine->GetYoungAllocationBytes()),
            "integrated survival snapshot starts exactly once");
    }

    void PolicyResume(bool, void* context)
    {
        static_cast<PolicyRuntime*>(context)->resumed.store(true, std::memory_order_release);
    }

    LxrRcResult PolicyRelease(LxrReferenceCounting*, void* context)
    {
        return static_cast<PolicyRuntime*>(context)->policy.ReleasePause()
            ? LxrRcResult::Success : LxrRcResult::InvariantViolation;
    }

    LxrRcResult PolicyLazy(LxrReferenceCounting* engine, void* context)
    {
        PolicyRuntime* runtime = static_cast<PolicyRuntime*>(context);
        size_t mature;
        if (!runtime->resumed.load(std::memory_order_acquire) ||
            engine->GetMaturePages(&mature) != LxrRcResult::Success ||
            ((runtime->pause == LxrPauseKind::Full || runtime->pause == LxrPauseKind::FinalMark) &&
                engine->GetTracePhase() != LxrTracePhase::Inactive) ||
            !runtime->policy.FinishLazySweep(runtime->pause, runtime->totalPages, mature,
                engine->GetTracePhase() == LxrTracePhase::Concurrent, true, true))
        {
            return LxrRcResult::InvariantViolation;
        }
        runtime->feeds.fetch_add(1, std::memory_order_release);
        return LxrRcResult::Success;
    }

    void ValidatePolicyIntegration()
    {
        LxrReferenceCounting engine;
        LxrRcConfiguration configuration{
            16 * ImmixBlockGeometry::BlockBytes, 8 * LxrLargeObjectSpace::MinimumObjectBytes,
            4, 1024, 4, 8,
        };
        PolicyRuntime policy{&engine, {}, LxrPauseKind::RefCount, true, 0,
            (configuration.immixBytes + configuration.largeObjectBytes) / LxrLargeObjectSpace::PageBytes};
        LxrPauseRuntimeAdapter runtime{PolicySuspend, PolicyResume, &policy, false};
        LxrRcObjectModel model{nullptr, false, nullptr, &policy, nullptr, PolicyRelease, nullptr, PolicyLazy};
        LxrRcMutator* mutator = nullptr;
        int identity;
        if (!Check(engine.Initialize(configuration, runtime, &model) == LxrRcResult::Success,
                "integrated policy model initializes") ||
            !Check(engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success, "policy mutator attaches"))
        {
            return;
        }
        Type type(24, 0);
        Object* object = Allocate(engine, mutator, type);
        std::vector<Object*> values{object};
        Roots roots{&values};
        for (LxrPauseKind pause : {
                LxrPauseKind::RefCount, LxrPauseKind::Full, LxrPauseKind::InitialMark, LxrPauseKind::FinalMark})
        {
            policy.pause = pause;
            size_t feeds = policy.feeds.load();
            double before = policy.policy.MatureLivePages();
            LxrRcResult result = pause == LxrPauseKind::Full ? engine.CollectFull(Capture, &roots, Timeout) :
                pause == LxrPauseKind::InitialMark ? engine.BeginTrace(Capture, &roots, Timeout) :
                pause == LxrPauseKind::FinalMark ? engine.FinishTrace(Capture, &roots, Timeout) :
                engine.Collect(Capture, &roots, Timeout);
            Check(result == LxrRcResult::Success && engine.DrainLazySweep(Timeout) == LxrRcResult::Success &&
                policy.feeds.load() == feeds + 1, "post-lazy policy feed runs exactly once before drainage returns");
            Check(pause == LxrPauseKind::Full || pause == LxrPauseKind::FinalMark
                    ? policy.policy.MatureLivePages() > before : policy.policy.MatureLivePages() == before,
                "only actual Full/FinalMark completion updates mature prediction");
        }
    }

    bool NativeAdmission(size_t, size_t, size_t bytes, void* context)
    {
        return bytes <= *static_cast<size_t*>(context);
    }

    void ValidateMinimumGrowthBudget()
    {
        LxrReferenceCounting engine;
        LxrRcConfiguration configuration{
            1024 * 1024, 1024 * 1024, 4, 1024, 4, 8, 2 * 1024 * 1024, 1024 * 1024, 3 * 1024 * 1024,
        };
        size_t limit = SIZE_MAX;
        LxrRcObjectModel model{};
        model.context = &limit;
        model.canAdmitRange = NativeAdmission;
        LxrPauseRuntimeAdapter runtime{Suspend, Resume, nullptr, false};
        LxrRcMutator* mutator = nullptr;
        int identity;
        if (!Check(engine.Initialize(configuration, runtime, &model) == LxrRcResult::Success,
                "minimum-growth budget fixture initializes") ||
            !Check(engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success, "budget mutator attaches"))
        {
            return;
        }
        Type type(32760, 0);
        std::vector<Object*> values;
        for (size_t index = 0; index < 32; index++)
        {
            values.push_back(Allocate(engine, mutator, type));
        }
        Object* next = nullptr;
        Check(engine.Allocate(mutator, 24, &next) == LxrRcResult::AllocationFailure,
            "retained objects exhaust initial Immix capacity");
        limit = engine.GetNativePlaneCommittedBytes() + 1024 * 1024;
        Roots roots{&values};
        Check(engine.Grow(false, 24, Capture, &roots, Timeout) == LxrRcResult::Success &&
            engine.GetImmixCapacityBytes() > configuration.immixBytes &&
            engine.GetImmixCapacityBytes() < 2 * configuration.immixBytes &&
            engine.GetNativePlaneCommittedBytes() <= limit,
            "affordable minimum growth succeeds when preferred doubling exceeds the native budget");
        Check(engine.Allocate(mutator, 24, &next) == LxrRcResult::Success,
            "smaller admitted growth makes the failed allocation succeed");
    }

    Object* Allocate(
        LxrReferenceCounting& engine, LxrRcMutator* mutator, Type& type)
    {
        Object* object = nullptr;
        if (!Check(engine.Allocate(mutator, type.table->GetBaseSize(), &object) ==
                LxrRcResult::Success, "product allocator hands out an object"))
        {
            return nullptr;
        }
        object->RawSetMethodTable(type.table);
        return object;
    }

    Object** Field(Object* object, size_t index)
    {
        return reinterpret_cast<Object**>(reinterpret_cast<uint8_t*>(object) + sizeof(void*)) + index;
    }

    bool Store(LxrReferenceCounting& engine, LxrRcMutator* mutator, Object** slot, Object* target)
    {
        Object* old = Interlocked::CompareExchangePointer(slot, static_cast<Object*>(nullptr), nullptr);
        if (engine.LogBeforeWrite(mutator, slot, old) != LxrRcResult::Success)
        {
            return false;
        }
        Interlocked::ExchangePointer(slot, target);
        return true;
    }

    uintptr_t Count(LxrReferenceCounting& engine, Object* object)
    {
        uintptr_t count = UINTPTR_MAX;
        Check(engine.LoadCount(object, &count) == LxrRcResult::Success, "RC metadata remains readable");
        return count;
    }

    void ValidateAcyclicAndRoots(uint8_t bits)
    {
        Type leaf(24, 0);
        Type link(24, 1);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, bits, &mutator, &identity))
        {
            return;
        }
        Object* parent = Allocate(engine, mutator, link);
        Object* child = Allocate(engine, mutator, leaf);
        if (parent == nullptr || child == nullptr)
        {
            return;
        }
        Check(Store(engine, mutator, Field(parent), child), "nursery field is initialized");
        if (!Epoch(engine, {parent}))
        {
            return;
        }
        if (!Check(Count(engine, parent) == 1 && Count(engine, child) == 1,
                "root increment recursively promotes the real GCDesc child"))
        {
            return;
        }
        Check(engine.GetMetrics().rootDecrements == 0,
            "current roots are not decremented in their own epoch");
        Object* containing = nullptr;
        Check(engine.FindContainingObject(reinterpret_cast<uintptr_t>(Field(parent)), &containing) ==
                LxrRcResult::Success && containing == parent,
            "interior root normalization respects the preceding ObjHeader");
        MethodTable* untouched = child->RawGetMethodTable();
        Check(Store(engine, mutator, Field(parent), nullptr), "old target is captured before overwrite");
        if (!Epoch(engine, {parent}))
        {
            return;
        }
        Check(Count(engine, parent) == 1 && Count(engine, child) == 0,
            "increment closure precedes old-root and old-field decrements");
        Check(child->RawGetMethodTable() == untouched, "death does not clear heap payload");
        Check(engine.GetMetrics().oldTargetChecks == 1, "zero-RC invariant has an executed reach count");
        if (!Epoch(engine, {}))
        {
            return;
        }
        Check(Count(engine, parent) == 0 && engine.GetMetrics().blocksReleased != 0,
            "acyclic graph returns an actual Immix block");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "mutator detaches");
    }

    void ValidateNurseryAndReuse(uint8_t bits)
    {
        Type leaf(24, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, bits, &mutator, &identity))
        {
            return;
        }
        for (size_t index = 0; index < 5; index++)
        {
            Object* object = Allocate(engine, mutator, leaf);
            if (object == nullptr)
            {
                return;
            }
            object->RawSetMethodTable(reinterpret_cast<MethodTable*>(static_cast<uintptr_t>(1)));
            uint64_t before = engine.GetMetrics().objectSizeQueries;
            if (!Epoch(engine, {}))
            {
                return;
            }
            Check(engine.GetMetrics().objectSizeQueries == before,
                "metadata-only nursery sweep never parses a poisoned object");
            Check(engine.GetMetrics().blocksReleased == index + 1,
                "repeated fresh allocation and reclamation retain no stale starts");
        }
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "reuse mutator detaches");
    }

    void ValidateStickyAndCycles(uint8_t bits)
    {
        Type link(24, 1);
        Type leaf(24, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, bits, &mutator, &identity))
        {
            return;
        }
        Object* stuck = Allocate(engine, mutator, leaf);
        Object* left = Allocate(engine, mutator, link);
        Object* right = Allocate(engine, mutator, link);
        if (stuck == nullptr || left == nullptr || right == nullptr)
        {
            return;
        }
        Check(Store(engine, mutator, Field(left), right) &&
            Store(engine, mutator, Field(right), left), "cycle nursery edges initialize");
        uintptr_t maximum = (static_cast<uintptr_t>(1) << bits) - 1;
        std::vector<Object*> roots(maximum, stuck);
        roots.push_back(left);
        if (!Epoch(engine, roots) || !Epoch(engine, {}))
        {
            return;
        }
        Check(Count(engine, stuck) == maximum, "sticky saturation is never decremented");
        Check(Count(engine, left) != 0 && Count(engine, right) != 0,
            "RC-only cycles remain retained pending P5");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "sticky mutator detaches");
    }

    void ValidateLargeObjects(uint8_t bits)
    {
        Type below(32760, 0);
        Type equal(32768, 0);
        Type above(32776, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, bits, &mutator, &identity))
        {
            return;
        }
        Object* belowThreshold = Allocate(engine, mutator, below);
        Object* large = Allocate(engine, mutator, equal);
        Object* larger = Allocate(engine, mutator, above);
        if (belowThreshold == nullptr || large == nullptr || larger == nullptr)
        {
            return;
        }
        if (!Epoch(engine, {belowThreshold, large, larger}) || !Epoch(engine, {}))
        {
            return;
        }
        Check(engine.GetMetrics().largeObjectBytesReleased >= 32768 + 36864,
            "inclusive LOS threshold promotes and releases independent page spans");
        Object* reused = Allocate(engine, mutator, equal);
        Check(reused == large, "released LOS pages are reused without stale object registration");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "LOS mutator detaches");
    }

    void ValidateOverflowAndExit(size_t fields)
    {
        Type leaf(24, 0);
        Type array(static_cast<uint32_t>(16 + fields * sizeof(void*)), fields);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Object* parent = Allocate(engine, mutator, array);
        if (parent == nullptr)
        {
            return;
        }
        for (size_t index = 0; index < fields; index++)
        {
            Object* child = Allocate(engine, mutator, leaf);
            if (child == nullptr)
            {
                return;
            }
            Check(Store(engine, mutator, Field(parent, index), child), "array nursery slot initializes");
        }
        if (!Epoch(engine, {parent}))
        {
            return;
        }
        std::atomic<size_t> errors{0};
        std::vector<std::thread> threads;
        for (size_t thread = 0; thread < 4; thread++)
        {
            threads.emplace_back([&, thread]()
            {
                int threadIdentity;
                LxrRcMutator* current = nullptr;
                if (engine.AttachMutator(&threadIdentity, &current) != LxrRcResult::Success)
                {
                    errors.fetch_add(1);
                    return;
                }
                for (size_t index = thread; index < fields; index += 4)
                {
                    if (!Store(engine, current, Field(parent, index), nullptr))
                    {
                        errors.fetch_add(1);
                    }
                }
                if (engine.DetachMutator(current) != LxrRcResult::Success)
                {
                    errors.fetch_add(1);
                }
            });
        }
        for (std::thread& thread : threads)
        {
            thread.join();
        }
        Check(errors.load() == 0, "multi-mutator exits flush every full and partial chunk");
        if (!Epoch(engine, {parent}))
        {
            return;
        }
        LxrRcMetrics metrics = engine.GetMetrics();
        Check(metrics.loggedSlots == fields && metrics.oldTargetChecks == fields,
            "every first pre-overwrite claim captures exactly one old target");
        Check(metrics.deaths == fields && metrics.incrementEntries == 2 * fields,
            "increment closure and recursive death consume all overflow entries");
        Check(metrics.threadFlushes == 4, "all departing mutators flush before identity reuse");
        Check(Epoch(engine, {}), "array root lease retires in a later epoch");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "array owner detaches");
    }

    void ValidateCompetingClaims()
    {
        Type leaf(24, 0);
        Type link(24, 1);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Object* parent = Allocate(engine, mutator, link);
        Object* initial = Allocate(engine, mutator, leaf);
        if (parent == nullptr || initial == nullptr)
        {
            return;
        }
        Check(Store(engine, mutator, Field(parent), initial), "racing-slot nursery edge initializes");
        if (!Epoch(engine, {parent}))
        {
            return;
        }
        for (size_t epoch = 0; epoch < 12; epoch++)
        {
            Object* before = *Field(parent);
            Object* left = Allocate(engine, mutator, leaf);
            Object* right = Allocate(engine, mutator, leaf);
            if (left == nullptr || right == nullptr)
            {
                return;
            }
            std::atomic<size_t> ready{0};
            std::atomic<size_t> errors{0};
            std::thread writers[2];
            for (size_t writer = 0; writer < 2; writer++)
            {
                writers[writer] = std::thread([&, writer]()
                {
                    int key;
                    LxrRcMutator* current;
                    if (engine.AttachMutator(&key, &current) != LxrRcResult::Success)
                    {
                        errors.fetch_add(1);
                    }
                    ready.fetch_add(1);
                    while (ready.load() != 2)
                    {
                        std::this_thread::yield();
                    }
                    if (current != nullptr &&
                        (!Store(engine, current, Field(parent), writer == 0 ? left : right) ||
                         engine.DetachMutator(current) != LxrRcResult::Success))
                    {
                        errors.fetch_add(1);
                    }
                });
            }
            for (std::thread& writer : writers)
            {
                writer.join();
            }
            Object* after = *Field(parent);
            uint64_t claims = engine.GetMetrics().loggedSlots;
            Check(errors.load() == 0 && claims == epoch + 1,
                "competing pre-overwrite writers win one claim and retain both contexts");
            if (!Epoch(engine, {parent}))
            {
                return;
            }
            Check(Count(engine, before) == 0 && Count(engine, after) == 1,
                "closure loads the final target, never a writer's proposed target");
        }
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "racing owner detaches");
    }

    int ZeroCountControl()
    {
        Type leaf(24, 0);
        Type link(24, 1);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return 2;
        }
        Object* parent = Allocate(engine, mutator, link);
        if (parent == nullptr || !Epoch(engine, {parent}))
        {
            return 2;
        }
        Object* child = Allocate(engine, mutator, leaf);
        if (child == nullptr)
        {
            return 2;
        }
        *Field(parent) = child;
        LxrRcResult result = engine.LogBeforeWrite(mutator, Field(parent), child);
        if (result == LxrRcResult::InvariantViolation &&
            engine.GetFailure() == result && *Field(parent) == child)
        {
            std::fprintf(stderr, "FAIL: injected zero-RC old target rejected before overwrite\n");
            return 1;
        }
        return 0;
    }

    int Benchmark()
    {
        Type leaf(24, 0);
        Type array(16 + 64 * sizeof(void*), 64);
        std::printf("policy,launch,barrier_ns,epoch_ns,allocated_bytes,deaths,blocks_released\n");
        volatile uint64_t injectedValue = 1;
        for (size_t launch = 0; launch < 3; launch++)
        {
            for (size_t policy = 0; policy < 3; policy++)
            {
                LxrReferenceCounting engine;
                int identity;
                LxrRcMutator* mutator;
                if (!Initialize(engine, 8, &mutator, &identity))
                {
                    return 1;
                }
                Object* parent = Allocate(engine, mutator, array);
                if (parent == nullptr || !Epoch(engine, {parent}))
                {
                    return 1;
                }
                std::vector<uint64_t> barrier;
                std::vector<uint64_t> epoch;
                for (size_t iteration = 0; iteration < 24; iteration++)
                {
                    for (size_t field = 0; field < 64; field++)
                    {
                        Object* child = Allocate(engine, mutator, leaf);
                        if (child == nullptr || !Store(engine, mutator, Field(parent, field), child))
                        {
                            return 1;
                        }
                    }
                    if (!Epoch(engine, {parent}))
                    {
                        return 1;
                    }
                    std::chrono::steady_clock::time_point start = std::chrono::steady_clock::now();
                    for (size_t field = 0; field < 64; field++)
                    {
                        if (!Store(engine, mutator, Field(parent, field), nullptr))
                        {
                            return 1;
                        }
                        if (policy == 2)
                        {
                            uint64_t value = injectedValue;
                            for (size_t cost = 0; cost < 4096; cost++)
                            {
                                value = value * 6364136223846793005ULL + 1;
                            }
                            injectedValue = value;
                        }
                    }
                    std::chrono::steady_clock::time_point logged = std::chrono::steady_clock::now();
                    if (!Epoch(engine, {parent}))
                    {
                        return 1;
                    }
                    std::chrono::steady_clock::time_point finished = std::chrono::steady_clock::now();
                    if (iteration >= 8)
                    {
                        barrier.push_back(std::chrono::duration_cast<std::chrono::nanoseconds>(
                            logged - start).count());
                        epoch.push_back(std::chrono::duration_cast<std::chrono::nanoseconds>(
                            finished - logged).count());
                    }
                }
                std::sort(barrier.begin(), barrier.end());
                std::sort(epoch.begin(), epoch.end());
                LxrRcMetrics metrics = engine.GetMetrics();
                std::printf("%s,%zu,%llu,%llu,%llu,%llu,%llu\n",
                    policy == 0 ? "base-a" : policy == 1 ? "base-b" : "injected",
                    launch,
                    static_cast<unsigned long long>(barrier[barrier.size() / 2]),
                    static_cast<unsigned long long>(epoch[epoch.size() / 2]),
                    static_cast<unsigned long long>(metrics.allocatedBytes),
                    static_cast<unsigned long long>(metrics.deaths),
                    static_cast<unsigned long long>(metrics.blocksReleased));
                if (metrics.deaths != 24 * 64)
                {
                    return 1;
                }
            }
        }
        return injectedValue == 0 ? 1 : 0;
    }

    void ValidateRecycledSizeFallback()
    {
        Type leaf(24, 0);
        Type nearThreshold(32760, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Object* retained = Allocate(engine, mutator, leaf);
        if (retained == nullptr || !Epoch(engine, {retained}))
        {
            return;
        }
        Object* large = Allocate(engine, mutator, nearThreshold);
        Check(large != nullptr,
            "too-small recycled tail advances to an available fresh block");
        if (large != nullptr)
        {
            Check(Epoch(engine, {retained, large}), "fresh fallback object promotes normally");
        }
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "fallback owner detaches");
    }

    void IgnoreUnlog(Object**, void*) {}
    void CountObservation(bool increment, uintptr_t, Object*, void* context)
    {
        size_t* counts = static_cast<size_t*>(context);
        counts[increment ? 0 : 1]++;
    }

    void ValidateChurnAdmission()
    {
        LxrReferenceCounting observer;
        Check(observer.InitializeObserver(8192, 32) == LxrRcResult::Success,
            "observational buffer owner initializes without an RC heap");
        Object* fields[32]{};
        size_t counts[2]{};
        int identity;
        for (size_t iteration = 0; iteration < 32; iteration++)
        {
            LxrRcMutator* mutator;
            Check(observer.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                "bounded sequential identity reuse is admitted");
            Check(observer.CaptureClaimedWrite(mutator, fields + iteration,
                    reinterpret_cast<Object*>(static_cast<uintptr_t>(0x1000))) == LxrRcResult::Success,
                "departing partial chunks are retained");
            Check(observer.DetachMutator(mutator) == LxrRcResult::Success,
                "sequential thread departure flushes partial chunks");
        }
        LxrRcMutator* rejected = nullptr;
        Check(observer.AttachMutator(&identity, &rejected) == LxrRcResult::Success &&
                observer.DetachMutator(rejected) == LxrRcResult::Success &&
                observer.GetFailure() == LxrRcResult::Success,
            "copied partial buffers allow identity reuse beyond the former epoch admission cap");
        Check(observer.DrainObservedLogs(IgnoreUnlog, CountObservation, counts) == LxrRcResult::Success &&
                counts[0] == 32 && counts[1] == 32,
            "all admitted partial chunks drain without loss");
        Check(observer.AttachMutator(&identity, &rejected) == LxrRcResult::Success &&
                observer.DetachMutator(rejected) == LxrRcResult::Success,
            "quiescent drain replenishes stream capacity without changing record admission");
    }

    void ValidateAllocationDuringLazyDeath()
    {
        Type leaf(24, 0);
        Type multiLine(1024, 0);
        Type replacement(768, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Object* kept = Allocate(engine, mutator, leaf);
        if (kept == nullptr)
        {
            return;
        }
        for (size_t iteration = 0; iteration < 32; iteration++)
        {
            Object* victim = Allocate(engine, mutator, multiLine);
            if (victim == nullptr || !Epoch(engine, {kept, victim}))
            {
                return;
            }
            std::vector<Object*> roots{kept};
            Roots captured{&roots};
            if (!Check(engine.Collect(Capture, &captured, Timeout) == LxrRcResult::Success,
                    "allocation may overlap a published lazy death"))
            {
                return;
            }
            Object* fresh = Allocate(engine, mutator, replacement);
            if (fresh == nullptr)
            {
                return;
            }
            Check(engine.Drain(Timeout) == LxrRcResult::Success &&
                    engine.GetFailure() == LxrRcResult::Success,
                "continuation publication cannot fault an allocating mutator");
            Check(Epoch(engine, {kept, fresh}), "object born during lazy work survives the next epoch");
        }
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "lazy allocator detaches");
    }

    void ValidateFirstStoreBeforeAllocation(bool concurrent = false)
    {
        constexpr size_t Fields = 64;
        Type array(16 + Fields * sizeof(void*), Fields);
        Type leaf(24, 0);
        LxrReferenceCounting engine;
        int owner;
        LxrRcMutator* allocatingMutator;
        if (!Initialize(engine, 8, &allocatingMutator, &owner))
        {
            return;
        }
        Object* parent = Allocate(engine, allocatingMutator, array);
        Object* child = Allocate(engine, allocatingMutator, leaf);
        if (parent == nullptr || child == nullptr)
        {
            return;
        }
        for (size_t index = 0; index < Fields; index++)
        {
            if (!Check(Store(engine, allocatingMutator, Field(parent, index), child),
                    "initial graph fields are populated"))
            {
                return;
            }
        }
        if (!Epoch(engine, {parent}))
        {
            return;
        }
        uint64_t allocations = engine.GetMetrics().allocations;
        uint64_t increments = engine.GetMetrics().incrementEntries;
        uint64_t decrements = engine.GetMetrics().decrementEntries;
        int identities[Fields]{};
        std::atomic<size_t> errors{0};
        size_t writersPerWave = concurrent ? 8 : 1;
        for (size_t first = 0; first < Fields; first += writersPerWave)
        {
            std::vector<std::thread> writers;
            for (size_t index = first; index < first + writersPerWave; index++)
            {
                writers.emplace_back([&, index]()
                {
                    LxrRcMutator* mutator = nullptr;
                    if (engine.AttachMutator(identities + index, &mutator) != LxrRcResult::Success)
                    {
                        errors.fetch_add(1);
                        return;
                    }
                    if (!Store(engine, mutator, Field(parent, index), nullptr) ||
                        engine.DetachMutator(mutator) != LxrRcResult::Success)
                    {
                        errors.fetch_add(1);
                    }
                });
            }
            for (std::thread& writer : writers)
            {
                writer.join();
            }
        }
        if (!Check(errors.load() == 0,
                "more than maximumMutators sequential first-store identities are admitted"))
        {
            return;
        }
        Check(engine.GetMetrics().allocations == allocations,
            "writer threads do not allocate before their first owned-heap store");
        Check(engine.GetMetrics().loggedSlots == Fields,
            "every first store claims exactly one owned slot");
        if (!Epoch(engine, {parent}))
        {
            return;
        }
        Check(engine.GetMetrics().incrementEntries - increments == Fields &&
                engine.GetMetrics().decrementEntries - decrements == Fields + 1,
            "detached partial buffers preserve all old targets and slots plus the root lease");
        Check(Count(engine, child) == 0,
            "all captured old edges are decremented after increment closure");
        Check(engine.DetachMutator(allocatingMutator) == LxrRcResult::Success,
            "first-store graph owner detaches");
    }

    bool TracePause(
        LxrReferenceCounting& engine, const std::vector<Object*>& objects, LxrPauseKind kind)
    {
        Roots roots{&objects};
        LxrRcResult result = kind == LxrPauseKind::InitialMark
            ? engine.BeginTrace(Capture, &roots, Timeout)
            : kind == LxrPauseKind::FinalMark
                ? engine.FinishTrace(Capture, &roots, Timeout)
                : engine.CollectFull(Capture, &roots, Timeout);
        if (!Check(result == LxrRcResult::Success, "requested trace pause completes"))
        {
            std::fprintf(stderr, "trace failure=%u phase=%u\n",
                static_cast<unsigned>(engine.GetFailure()), static_cast<unsigned>(engine.GetTracePhase()));
            return false;
        }
        return kind == LxrPauseKind::InitialMark ||
            Check(engine.Drain(Timeout) == LxrRcResult::Success, "trace release ownership drains");
    }

    // P28 (F10). Counts of 2 bits are the default, so a count reaches its maximum (3) with a few references. More references than
    // the maximum must leave the count at the maximum (no wrap to a small value), the object must then stay alive through all
    // decrements (sticky), and only the trace may reclaim it. The control makes an increment at the maximum wrap to zero.
    void ValidateF10Overflow(uint8_t bits)
    {
        Type leaf(24, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, bits, &mutator, &identity))
        {
            return;
        }
        Object* shared = Allocate(engine, mutator, leaf);
        Object* single = Allocate(engine, mutator, leaf);
        Object* neighbour = Allocate(engine, mutator, leaf);
        if (shared == nullptr || single == nullptr || neighbour == nullptr)
        {
            return;
        }
        uintptr_t maximum = (static_cast<uintptr_t>(1) << bits) - 1;
        std::vector<Object*> roots(maximum + 7, shared);
        roots.insert(roots.begin() + 2, single);
        roots.push_back(neighbour);
        if (!Epoch(engine, roots))
        {
            return;
        }
        Check(Count(engine, shared) == maximum, "more references than the maximum leave the count at the maximum");
        Check(Count(engine, single) == 1 && Count(engine, neighbour) == 1,
            "the counts next to a saturated count are not changed");
        if (!Epoch(engine, {}))
        {
            return;
        }
        Check(Count(engine, single) == 0 && Count(engine, neighbour) == 0, "ordinary counts reach zero");
        Check(Count(engine, shared) == maximum && engine.IsObjectStart(shared),
            "a stuck object survives every decrement");
        if (!TracePause(engine, {}, LxrPauseKind::Full))
        {
            return;
        }
        Check(Count(engine, shared) == 0 && !engine.IsObjectStart(shared),
            "the trace reclaims a stuck object that is not reachable");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "overflow owner detaches");
    }

    void ValidateCycleReclamation(uint8_t bits)
    {
        Type link(24, 1);
        Type leaf(24, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, bits, &mutator, &identity))
        {
            return;
        }
        Object* liveLeft = Allocate(engine, mutator, link);
        Object* liveRight = Allocate(engine, mutator, link);
        Object* deadLeft = Allocate(engine, mutator, link);
        Object* deadRight = Allocate(engine, mutator, link);
        Object* self = Allocate(engine, mutator, link);
        Object* stuck = Allocate(engine, mutator, leaf);
        if (liveLeft == nullptr || liveRight == nullptr || deadLeft == nullptr ||
            deadRight == nullptr || self == nullptr || stuck == nullptr)
        {
            return;
        }
        Check(Store(engine, mutator, Field(liveLeft), liveRight) &&
                Store(engine, mutator, Field(liveRight), liveLeft) &&
                Store(engine, mutator, Field(deadLeft), deadRight) &&
                Store(engine, mutator, Field(deadRight), deadLeft) &&
                Store(engine, mutator, Field(self), self),
            "reachable, unreachable and self-cycle fields initialize");
        uintptr_t maximum = (static_cast<uintptr_t>(1) << bits) - 1;
        std::vector<Object*> roots(maximum, stuck);
        roots.insert(roots.end(), {liveLeft, deadLeft, self});
        if (!Epoch(engine, roots) || !Epoch(engine, {liveLeft}))
        {
            return;
        }
        uint64_t queries = engine.GetMetrics().objectSizeQueries;
        // The cycle sweep reads the size of a dead object from the object (rule 4 was relaxed in P17 S3), so the
        // types stay valid until the sweep ends. The runtime frees type data only after that: see ValidateCycleSweepGate.
        if (!TracePause(engine, {liveLeft}, LxrPauseKind::Full))
        {
            return;
        }
        Check(!engine.IsCycleSweepPending(), "the cycle sweep has finished when the trace release drains");
        deadLeft->RawSetMethodTable(reinterpret_cast<MethodTable*>(static_cast<uintptr_t>(1)));
        deadRight->RawSetMethodTable(reinterpret_cast<MethodTable*>(static_cast<uintptr_t>(1)));
        self->RawSetMethodTable(reinterpret_cast<MethodTable*>(static_cast<uintptr_t>(1)));
        stuck->RawSetMethodTable(reinterpret_cast<MethodTable*>(static_cast<uintptr_t>(1)));
        LxrTraceMetrics metrics = engine.GetTraceMetrics();
        Check(metrics.cycleObjects == 4 && metrics.cycleBytes == 96,
            "tracing reclaims both dead-cycle nodes, self-cycle and saturation with sizes read from the objects");
        Check(engine.GetLiveBytes() == 48 && engine.GetReclaimedBytes() == 96,
            "actual owned birth extents conserve live and reclaimed payload bytes");
        Check(engine.GetMetrics().objectSizeQueries == queries,
            "the dead mature cycle sweep reads sizes without the counted size query");
        Check(*Field(liveLeft) == liveRight && *Field(liveRight) == liveLeft &&
                liveLeft->RawGetMethodTable() == link.table && liveRight->RawGetMethodTable() == link.table,
            "reachable cycle payloads and MethodTables survive the same full trace");
        Check(Count(engine, deadLeft) == 0 && Count(engine, deadRight) == 0 &&
                Count(engine, self) == 0 && Count(engine, stuck) == 0,
            "cycle and sticky counts are cleared without recursive edge decrements");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "cycle owner detaches");
    }

    void ValidateMarkAndScanOnDelete()
    {
        Type link(24, 1);
        Type leaf(24, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Object* parent = Allocate(engine, mutator, link);
        Object* child = Allocate(engine, mutator, link);
        Object* descendant = Allocate(engine, mutator, leaf);
        if (parent == nullptr || child == nullptr || descendant == nullptr)
        {
            return;
        }
        Check(Store(engine, mutator, Field(parent), child) &&
                Store(engine, mutator, Field(child), descendant),
            "deletion race graph initializes");
        if (!Epoch(engine, {parent}) ||
            !Check(engine.HoldTrace(true) == LxrRcResult::Success, "deterministic trace gate arms") ||
            !TracePause(engine, {parent}, LxrPauseKind::InitialMark))
        {
            return;
        }
        Check(engine.GetTracePhase() == LxrTracePhase::Concurrent && !engine.IsMarked(parent),
            "InitialMark activates tracing at pause end while the parent is still unmarked");
        uint64_t scans = engine.GetMetrics().objectScans;
        std::vector<Object*> empty;
        Roots roots{&empty};
        if (!Check(engine.Collect(Capture, &roots, Timeout) == LxrRcResult::Success,
                "RC pause interrupts the held trace") ||
            !Check(engine.GetPhase() == LxrRcPhase::Mutator,
                "RC pause restores the mutator phase without abandoning tracing"))
        {
            return;
        }
        if (!Check(engine.Drain(Timeout) == LxrRcResult::Success,
                "held trace permits deterministic draining of RC ownership") ||
            !Check(engine.GetLiveBytes() == 0, "lazy deletion reclaims the three owned extents"))
        {
            return;
        }
        Check(engine.GetTraceMetrics().deletionMarks == 3 &&
                engine.GetMetrics().objectScans - scans == 3,
            "unmarked parent and children are marked and scanned before zero publication");
        parent->RawSetMethodTable(reinterpret_cast<MethodTable*>(static_cast<uintptr_t>(1)));
        child->RawSetMethodTable(reinterpret_cast<MethodTable*>(static_cast<uintptr_t>(1)));
        descendant->RawSetMethodTable(reinterpret_cast<MethodTable*>(static_cast<uintptr_t>(1)));
        if (!TracePause(engine, {}, LxrPauseKind::FinalMark))
        {
            return;
        }
        Check(engine.GetTracePhase() == LxrTracePhase::Inactive &&
                engine.GetTraceMetrics().finalMarks == 1 &&
                engine.GetTraceMetrics().interruptions == 2,
            "FinalMark finishes held snapshot work and retires the trace");
        Check(engine.GetReclaimedBytes() == 72,
            "snapshot packets neither reread reclaimed payloads nor double-account deaths");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "deletion race owner detaches");
    }

    void ValidateSharedSatbStream()
    {
        constexpr size_t Fields = 4097;
        Type array(16 + Fields * sizeof(void*), Fields);
        Type leaf(24, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Object* parent = Allocate(engine, mutator, array);
        Object* child = Allocate(engine, mutator, leaf);
        if (parent == nullptr || child == nullptr)
        {
            return;
        }
        for (size_t index = 0; index < Fields; index++)
        {
            if (!Check(Store(engine, mutator, Field(parent, index), child),
                    "large reference layout initializes"))
            {
                return;
            }
        }
        if (!Epoch(engine, {parent}) ||
            !Check(engine.HoldTrace(true) == LxrRcResult::Success, "shared-stream gate arms") ||
            !TracePause(engine, {parent}, LxrPauseKind::InitialMark))
        {
            return;
        }
        for (size_t index = 0; index < Fields; index++)
        {
            if (!Check(Store(engine, mutator, Field(parent, index), nullptr),
                    "concurrent snapshot deletion logs its old value"))
            {
                return;
            }
        }
        if (!TracePause(engine, {parent}, LxrPauseKind::FinalMark))
        {
            return;
        }
        LxrTraceMetrics metrics = engine.GetTraceMetrics();
        Check(metrics.satbEntries == Fields && engine.IsMarked(child),
            "SATB owns every copied old target independently of the RC consumer");
        Check(metrics.splitPackets >= 4,
            "large-object scans split at actual reference-packet boundaries");
        Check(engine.GetLiveBytes() == array.table->GetBaseSize() + 24,
            "SATB conservatively retains the overwritten initial-snapshot target");
        if (!TracePause(engine, {}, LxrPauseKind::Full))
        {
            return;
        }
        Check(engine.GetLiveBytes() == 0 &&
                engine.GetReclaimedBytes() == array.table->GetBaseSize() + 24 &&
                engine.GetMetrics().largeObjectBytesReleased != 0,
            "next full trace reclaims the saturated target and actual LOS pages");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "shared-stream owner detaches");
    }

    struct FlaggedRoots
    {
        Object* first;
        Object* second;
        bool traceFirst;
    };

    LxrRcResult CaptureFlagged(LxrReferenceCounting* engine, void* context)
    {
        FlaggedRoots* roots = static_cast<FlaggedRoots*>(context);
        LxrRcResult result = engine->AddRoot(roots->first, false, roots->traceFirst);
        return result == LxrRcResult::Success ? engine->AddRoot(roots->second) : result;
    }

    void ValidateRetiredLeaseReuse()
    {
        Type leaf(24, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        LxrRcConfiguration configuration{
            ImmixBlockGeometry::BlockBytes, LxrLargeObjectSpace::MinimumObjectBytes,
            16, 1024, 4, 8,
        };
        LxrPauseRuntimeAdapter runtime{Suspend, Resume, nullptr, false};
        if (!Check(engine.Initialize(configuration, runtime) == LxrRcResult::Success,
                "one-block exact-address-reuse engine initializes") ||
            !Check(engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                "exact-address-reuse owner attaches"))
        {
            return;
        }
        Object* old = Allocate(engine, mutator, leaf);
        if (old == nullptr)
        {
            return;
        }
        FlaggedRoots leased{old, nullptr, false};
        if (!Check(engine.Collect(CaptureFlagged, &leased, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "nontracing weak lease promotes its referent") ||
            !Check(engine.CollectFull(CaptureFlagged, &leased, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "unmarked leased allocation is swept"))
        {
            return;
        }
        Object* replacement = Allocate(engine, mutator, leaf);
        Check(replacement == old, "replacement reuses the exact swept weak-lease address");
        if (replacement == nullptr || !Epoch(engine, {replacement}))
        {
            return;
        }
        Check(Count(engine, replacement) == 1 && engine.GetLiveBytes() == 24,
            "retired lease does not decrement a distinct replacement allocation");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "lease-reuse owner detaches");
    }

    void ValidateBlackNewbornClosure()
    {
        Type link(24, 1);
        Type leaf(24, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Object* old = Allocate(engine, mutator, leaf);
        Object* guard = Allocate(engine, mutator, leaf);
        if (old == nullptr || guard == nullptr)
        {
            return;
        }
        FlaggedRoots initial{old, guard, false};
        if (!Check(engine.Collect(CaptureFlagged, &initial, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "older white target has a nontracing lease") ||
            !Check(engine.HoldTrace(true) == LxrRcResult::Success &&
                engine.BeginTrace(CaptureFlagged, &initial, Timeout) == LxrRcResult::Success,
                "trace starts without marking a weak-only target"))
        {
            return;
        }
        Object* newborn = Allocate(engine, mutator, link);
        if (newborn == nullptr)
        {
            return;
        }
        Check(Store(engine, mutator, Field(newborn), old), "black newborn points to an older white target");
        FlaggedRoots final{newborn, newborn, false};
        if (!Check(engine.FinishTrace(CaptureFlagged, &final, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
                "nontracing-first newborn promotion closes before FinalMark sweep"))
        {
            return;
        }
        Check(engine.GetObjectExtent(old) == 24 && engine.IsMarked(old) && *Field(newborn) == old,
            "allocation-marked newborn cannot hide an unmarked reachable child");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "black-newborn owner detaches");
    }

    // P25 (F1) fixtures. A moving root set drives every pause. In mode "nursery" the pauses copy young objects, so a test
    // follows an object from the roots and never keeps its address.
    struct F1Fixture
    {
        LxrReferenceCounting engine;
        MovingRoots roots;
        LxrRcObjectModel model{};
        LxrRcMutator* mutator = nullptr;
        int identity = 0;
    };

    bool InitializeF1(F1Fixture& fixture, bool nursery)
    {
        LxrRcConfiguration configuration{
            64 * ImmixBlockGeometry::BlockBytes, 8 * LxrLargeObjectSpace::MinimumObjectBytes, 4, 2048, 4, 8,
        };
        configuration.evacuateNursery = nursery;
        fixture.model.context = &fixture.roots;
        fixture.model.finishRelocation = RelocateMoving;
        LxrPauseRuntimeAdapter runtime{Suspend, Resume, nullptr, false};
        return Check(fixture.engine.Initialize(configuration, runtime, &fixture.model) == LxrRcResult::Success &&
                fixture.engine.AttachMutator(&fixture.identity, &fixture.mutator) == LxrRcResult::Success,
            "F1 fixture engine and mutator initialize");
    }

    bool F1Pause(F1Fixture& fixture, LxrPauseKind kind)
    {
        LxrReferenceCounting& engine = fixture.engine;
        LxrRcResult result = kind == LxrPauseKind::InitialMark
            ? engine.BeginTrace(CaptureMoving, &fixture.roots, Timeout)
            : kind == LxrPauseKind::FinalMark
                ? engine.FinishTrace(CaptureMoving, &fixture.roots, Timeout)
                : kind == LxrPauseKind::Full
                    ? engine.CollectFull(CaptureMoving, &fixture.roots, Timeout)
                    : engine.Collect(CaptureMoving, &fixture.roots, Timeout);
        if (!Check(result == LxrRcResult::Success, "F1 pause completes"))
        {
            std::fprintf(stderr, "F1 pause failure=%u\n", static_cast<unsigned>(engine.GetFailure()));
            return false;
        }
        return kind == LxrPauseKind::InitialMark ||
            Check(engine.Drain(Timeout) == LxrRcResult::Success, "F1 lazy work drains");
    }

    // A pause counts a black newborn without a trace packet. The newborns are allocated after the snapshot, while the
    // trace is held, and a RefCount pause promotes them. Only the root of the pause may get a packet. The trace then
    // ends and every newborn is still there, because allocation marked it (Java LXR: rc.rs promote).
    void ValidateF1BlackAllocation(bool nursery)
    {
        F1Fixture fixture;
        if (!InitializeF1(fixture, nursery))
        {
            return;
        }
        LxrReferenceCounting& engine = fixture.engine;
        Type link(24, 1);
        Type leaf(24, 0);
        Object* anchor = Allocate(engine, fixture.mutator, link);
        Object* old = Allocate(engine, fixture.mutator, leaf);
        if (anchor == nullptr || old == nullptr ||
            !Check(Store(engine, fixture.mutator, Field(anchor), old), "F1 anchor points to an old target"))
        {
            return;
        }
        fixture.roots.objects = {anchor};
        if (!F1Pause(fixture, LxrPauseKind::RefCount) ||
            !Check(engine.HoldTrace(true) == LxrRcResult::Success, "F1 trace gate arms") ||
            !F1Pause(fixture, LxrPauseKind::InitialMark))
        {
            return;
        }
        Check(engine.GetTracePhase() == LxrTracePhase::Concurrent, "F1 trace is concurrent");
        anchor = fixture.roots.objects[0];
        constexpr size_t Chain = 300;
        Object* previous = nullptr;
        Object* first = nullptr;
        for (size_t index = 0; index < Chain; index++)
        {
            Object* node = Allocate(engine, fixture.mutator, link);
            if (node == nullptr)
            {
                return;
            }
            if (previous != nullptr && !Check(Store(engine, fixture.mutator, Field(previous), node),
                    "F1 newborn chain link stores"))
            {
                return;
            }
            first = first == nullptr ? node : first;
            previous = node;
        }
        if (!Check(Store(engine, fixture.mutator, Field(anchor), first), "F1 anchor points to the newborn chain"))
        {
            return;
        }
        uint64_t before = engine.GetStorageMetric(57);
        if (!F1Pause(fixture, LxrPauseKind::RefCount))
        {
            return;
        }
        uint64_t queued = engine.GetStorageMetric(57) - before;
        std::printf("f1_black nursery=%d chain=%zu increment_packets=%llu\n", nursery ? 1 : 0, Chain,
            static_cast<unsigned long long>(queued));
        Check(queued <= fixture.roots.objects.size(),
            "a black newborn that a pause counts gets no trace packet (only the root of the pause may get one)");
        anchor = fixture.roots.objects[0];
        size_t walked = 0;
        size_t marked = 0;
        for (Object* node = *Field(anchor); node != nullptr && walked < Chain + 1; node = *Field(node))
        {
            walked++;
            marked += engine.IsMarked(node) && Count(engine, node) != 0 ? 1 : 0;
        }
        Check(walked == Chain && marked == Chain, "every newborn is counted and marked after the RefCount pause");
        if (!Check(engine.HoldTrace(false) == LxrRcResult::Success, "F1 trace gate opens") ||
            !F1Pause(fixture, LxrPauseKind::FinalMark))
        {
            return;
        }
        anchor = fixture.roots.objects[0];
        walked = 0;
        size_t alive = 0;
        for (Object* node = *Field(anchor); node != nullptr && walked < Chain + 1; node = *Field(node))
        {
            walked++;
            alive += engine.GetObjectExtent(node) == 24 && Count(engine, node) != 0 ? 1 : 0;
        }
        Check(walked == Chain && alive == Chain && engine.GetTraceMetrics().cycleObjects == 0,
            "no newborn is swept as a dead cycle when the trace ends");
        Check(engine.DetachMutator(fixture.mutator) == LxrRcResult::Success, "F1 black fixture detaches");
    }

    // The InitialMark pause counts young objects that are not marked, and it queues no packet for them. The trace must
    // still mark them, from the roots, and the cycle sweep must free only the dead cycle (Java LXR: only roots are queued).
    void ValidateF1RootsOnly(bool nursery)
    {
        F1Fixture fixture;
        if (!InitializeF1(fixture, nursery))
        {
            return;
        }
        LxrReferenceCounting& engine = fixture.engine;
        Type pair(32, 2);
        Object* anchor = Allocate(engine, fixture.mutator, pair);
        Object* a = Allocate(engine, fixture.mutator, pair);
        Object* b = Allocate(engine, fixture.mutator, pair);
        Object* x = Allocate(engine, fixture.mutator, pair);
        Object* y = Allocate(engine, fixture.mutator, pair);
        if (anchor == nullptr || a == nullptr || b == nullptr || x == nullptr || y == nullptr ||
            !Check(Store(engine, fixture.mutator, Field(anchor, 0), a) &&
                    Store(engine, fixture.mutator, Field(a, 0), b) &&
                    Store(engine, fixture.mutator, Field(x, 0), y) &&
                    Store(engine, fixture.mutator, Field(y, 0), x),
                "F1 root graph and dead-cycle candidates initialize"))
        {
            return;
        }
        fixture.roots.objects = {anchor, x};
        if (!F1Pause(fixture, LxrPauseKind::RefCount))
        {
            return;
        }
        fixture.roots.objects = {fixture.roots.objects[0]};
        if (!F1Pause(fixture, LxrPauseKind::RefCount))
        {
            return;
        }
        anchor = fixture.roots.objects[0];
        Object* p = Allocate(engine, fixture.mutator, pair);
        Object* q = Allocate(engine, fixture.mutator, pair);
        if (p == nullptr || q == nullptr ||
            !Check(Store(engine, fixture.mutator, Field(p, 0), q) &&
                    Store(engine, fixture.mutator, Field(anchor, 1), p),
                "F1 young pair hangs on a mature slot"))
        {
            return;
        }
        if (!Check(engine.HoldTrace(true) == LxrRcResult::Success, "F1 root-only gate arms"))
        {
            return;
        }
        uint64_t before = engine.GetStorageMetric(57);
        if (!F1Pause(fixture, LxrPauseKind::InitialMark))
        {
            return;
        }
        uint64_t queued = engine.GetStorageMetric(57) - before;
        std::printf("f1_roots nursery=%d increment_packets=%llu\n", nursery ? 1 : 0,
            static_cast<unsigned long long>(queued));
        Check(queued == 0, "InitialMark queues no trace packet for a counted object (the roots are queued by the pause)");
        if (!Check(engine.HoldTrace(false) == LxrRcResult::Success, "F1 root-only gate opens") ||
            !F1Pause(fixture, LxrPauseKind::FinalMark))
        {
            return;
        }
        anchor = fixture.roots.objects[0];
        Object* childA = *Field(anchor, 0);
        Object* childP = *Field(anchor, 1);
        Object* childB = childA == nullptr ? nullptr : *Field(childA, 0);
        Object* childQ = childP == nullptr ? nullptr : *Field(childP, 0);
        Check(childA != nullptr && childB != nullptr && childP != nullptr && childQ != nullptr &&
                engine.IsMarked(childA) && engine.IsMarked(childB) && engine.IsMarked(childP) &&
                engine.IsMarked(childQ) &&
                engine.GetObjectExtent(childB) == 32 && engine.GetObjectExtent(childQ) == 32,
            "objects that were counted unmarked at InitialMark are marked by the trace from the roots");
        Check(engine.GetTraceMetrics().cycleObjects == 2,
            "the cycle sweep frees exactly the dead cycle and no object that is reachable only through a skipped packet");
        Check(engine.DetachMutator(fixture.mutator) == LxrRcResult::Success, "F1 roots fixture detaches");
    }

    // The trace from the roots of InitialMark is what protects an object that only a root holds at the snapshot (Java LXR:
    // rc.rs ProcessIncs, the roots of InitialMark go to LXRConcurrentTraceObjects). The program then stores that object into
    // a mature object that the trace has already scanned. The store is not a deletion, so only the first trace can have
    // marked the object. FinalMark must find it live.
    void ValidateF1SnapshotRoot()
    {
        F1Fixture fixture;
        if (!InitializeF1(fixture, false))
        {
            return;
        }
        LxrReferenceCounting& engine = fixture.engine;
        Type pair(32, 2);
        Type leaf(24, 0);
        Object* anchor = Allocate(engine, fixture.mutator, pair);
        Object* mature = Allocate(engine, fixture.mutator, pair);
        if (anchor == nullptr || mature == nullptr ||
            !Check(Store(engine, fixture.mutator, Field(anchor, 0), mature), "F1 snapshot graph initializes"))
        {
            return;
        }
        fixture.roots.objects = {anchor};
        if (!F1Pause(fixture, LxrPauseKind::RefCount))
        {
            return;
        }
        Object* held = Allocate(engine, fixture.mutator, leaf);
        if (held == nullptr)
        {
            return;
        }
        fixture.roots.objects = {anchor, held};
        if (!F1Pause(fixture, LxrPauseKind::InitialMark))
        {
            return;
        }
        auto deadline = std::chrono::steady_clock::now() + std::chrono::seconds(20);
        while (!engine.IsTraceExhausted() && std::chrono::steady_clock::now() < deadline)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(1));
        }
        Check(engine.IsTraceExhausted() && engine.IsMarked(held),
            "the concurrent trace marks an object that only a root of InitialMark holds");
        Check(Store(engine, fixture.mutator, Field(mature, 1), held), "F1 root object moves into a scanned mature object");
        fixture.roots.objects = {anchor};
        if (!F1Pause(fixture, LxrPauseKind::FinalMark))
        {
            return;
        }
        Check(engine.GetObjectExtent(held) == 24 && Count(engine, held) != 0 && *Field(mature, 1) == held &&
                engine.GetTraceMetrics().cycleObjects == 0,
            "the object that moved from a snapshot root into a scanned object survives the cycle sweep");
        Check(engine.DetachMutator(fixture.mutator) == LxrRcResult::Success, "F1 snapshot fixture detaches");
    }

    // Random graph with interleaved RefCount, InitialMark, FinalMark and Full pauses and a running trace. The model knows
    // what is reachable. After every pause each reachable object must be counted, owned and still linked as in the model.
    void ValidateF1Stress(uint32_t seed)
    {
        F1Fixture fixture;
        if (!InitializeF1(fixture, false))
        {
            return;
        }
        LxrReferenceCounting& engine = fixture.engine;
        Type node(32, 2);
        std::mt19937 random(seed);
        std::vector<Object*> object;
        std::vector<std::array<int, 2>> field;
        std::vector<int> root;
        auto reach = [&]()
        {
            std::vector<bool> seen(object.size(), false);
            std::vector<int> stack(root.begin(), root.end());
            while (!stack.empty())
            {
                int index = stack.back();
                stack.pop_back();
                if (index < 0 || seen[index])
                {
                    continue;
                }
                seen[index] = true;
                for (int target : field[index])
                {
                    stack.push_back(target);
                }
            }
            return seen;
        };
        auto pick = [&](const std::vector<int>& from) { return from[random() % from.size()]; };
        auto liveList = [&](const std::vector<bool>& seen)
        {
            std::vector<int> list;
            for (size_t index = 0; index < seen.size(); index++)
            {
                if (seen[index])
                {
                    list.push_back(static_cast<int>(index));
                }
            }
            return list;
        };
        auto link = [&](int source, size_t slot, int target)
        {
            field[source][slot] = target;
            return Store(engine, fixture.mutator, Field(object[source], slot), target < 0 ? nullptr : object[target]);
        };
        Object* first = Allocate(engine, fixture.mutator, node);
        if (first == nullptr)
        {
            return;
        }
        object.push_back(first);
        field.push_back({-1, -1});
        root.push_back(0);
        size_t traces = 0;
        size_t fulls = 0;
        for (size_t round = 0; round < 60; round++)
        {
            std::vector<int> live = liveList(reach());
            size_t newborn = 20 + random() % 40;
            for (size_t count = 0; count < newborn; count++)
            {
                Object* created = Allocate(engine, fixture.mutator, node);
                if (created == nullptr)
                {
                    return;
                }
                int index = static_cast<int>(object.size());
                object.push_back(created);
                field.push_back({-1, -1});
                int parent = pick(live);
                if (!Check(link(parent, random() % 2, index), "F1 stress newborn links"))
                {
                    return;
                }
                if (random() % 2 == 0 && !Check(link(index, random() % 2, pick(live)), "F1 stress newborn points back"))
                {
                    return;
                }
                live.push_back(index);
            }
            size_t changes = 20 + random() % 40;
            for (size_t count = 0; count < changes; count++)
            {
                int source = pick(live);
                int target = random() % 4 == 0 ? -1 : pick(live);
                if (!Check(link(source, random() % 2, target), "F1 stress store"))
                {
                    return;
                }
            }
            std::vector<bool> seen = reach();
            std::vector<int> reachable = liveList(seen);
            root.clear();
            size_t roots = 1 + random() % 5;
            for (size_t count = 0; count < roots; count++)
            {
                root.push_back(pick(reachable));
            }
            seen = reach();
            for (size_t index = 0; index < object.size(); index++)
            {
                if (!seen[index])
                {
                    object[index] = nullptr;
                    field[index] = {-1, -1};
                }
            }
            fixture.roots.objects.clear();
            for (int index : root)
            {
                fixture.roots.objects.push_back(object[index]);
            }
            LxrTracePhase phase = engine.GetTracePhase();
            uint32_t choice = random() % 10;
            LxrPauseKind kind = LxrPauseKind::RefCount;
            if (phase == LxrTracePhase::Inactive)
            {
                kind = choice < 4 ? LxrPauseKind::RefCount : choice < 8 ? LxrPauseKind::InitialMark
                    : choice < 9 ? LxrPauseKind::RefCount : LxrPauseKind::Full;
            }
            else if (phase == LxrTracePhase::Concurrent)
            {
                kind = choice < 5 ? LxrPauseKind::RefCount : LxrPauseKind::FinalMark;
            }
            traces += kind == LxrPauseKind::InitialMark ? 1 : 0;
            fulls += kind == LxrPauseKind::Full || kind == LxrPauseKind::FinalMark ? 1 : 0;
            if (!F1Pause(fixture, kind))
            {
                return;
            }
            size_t wrong = 0;
            for (size_t index = 0; index < object.size(); index++)
            {
                if (object[index] == nullptr)
                {
                    continue;
                }
                bool ok = Count(engine, object[index]) != 0 && engine.GetObjectExtent(object[index]) == 32 &&
                    engine.IsObjectStart(object[index]);
                for (size_t slot = 0; slot < 2; slot++)
                {
                    int target = field[index][slot];
                    ok = ok && *Field(object[index], slot) == (target < 0 ? nullptr : object[target]);
                }
                wrong += ok ? 0 : 1;
            }
            if (!Check(wrong == 0, "F1 stress: every reachable object is counted, owned and linked after the pause"))
            {
                std::fprintf(stderr, "seed %u round %zu kind %u wrong %zu\n", seed, round,
                    static_cast<unsigned>(kind), wrong);
                return;
            }
        }
        std::printf("f1_stress seed=%u initial_marks=%zu final_or_full=%zu\n", seed, traces, fulls);
        Check(traces >= 3 && fulls >= 3, "F1 stress runs complete trace cycles");
        Check(engine.DetachMutator(fixture.mutator) == LxrRcResult::Success, "F1 stress fixture detaches");
    }

    // P26 (F8). The thread that made a pause does not wait for the lazy decrements and the lazy sweep (the Java LXR runs them on
    // its workers while the program runs). This test is the F1 stress with no Drain after the pause: the program allocates,
    // stores and reads while the lazy work of the last pause is still pending (each lazy packet sleeps first). After every
    // pause each reachable object must be counted, owned and linked as in the model, and no new object may take the memory
    // of a reachable one. At the end a Full pause and a drain must leave exactly the reachable objects alive (the oracle
    // knows every byte). Control LXR_F8_WAIT_CONTROL: the pause drains, so no round can overlap with lazy work, and the
    // overlap check fails.
    void ValidateF8Stress(uint32_t seed, bool nursery)
    {
        F1Fixture fixture;
        if (!InitializeF1(fixture, nursery))
        {
            return;
        }
        LxrReferenceCounting& engine = fixture.engine;
        engine.SetLazyDelayForTest(300);
        Type node(32, 2);
        std::mt19937 random(seed);
        std::vector<Object*> object;
        std::vector<std::array<int, 2>> field;
        std::vector<int> root;
        auto reach = [&]()
        {
            std::vector<bool> seen(object.size(), false);
            std::vector<int> stack(root.begin(), root.end());
            while (!stack.empty())
            {
                int index = stack.back();
                stack.pop_back();
                if (index < 0 || seen[index])
                {
                    continue;
                }
                seen[index] = true;
                for (int target : field[index])
                {
                    stack.push_back(target);
                }
            }
            return seen;
        };
        auto pick = [&](const std::vector<int>& from) { return from[random() % from.size()]; };
        auto liveList = [&](const std::vector<bool>& seen)
        {
            std::vector<int> list;
            for (size_t index = 0; index < seen.size(); index++)
            {
                if (seen[index])
                {
                    list.push_back(static_cast<int>(index));
                }
            }
            return list;
        };
        auto link = [&](int source, size_t slot, int target)
        {
            field[source][slot] = target;
            return Store(engine, fixture.mutator, Field(object[source], slot), target < 0 ? nullptr : object[target]);
        };
        Object* first = Allocate(engine, fixture.mutator, node);
        if (first == nullptr)
        {
            return;
        }
        object.push_back(first);
        field.push_back({-1, -1});
        root.push_back(0);
        size_t overlapRounds = 0;
        size_t traces = 0;
        size_t cycleSweeps = 0;
        for (size_t round = 0; round < 60; round++)
        {
            overlapRounds += engine.GetLazyPendingForTest() != 0 ? 1 : 0;
            std::vector<int> live = liveList(reach());
            size_t newborn = 20 + random() % 40;
            for (size_t count = 0; count < newborn; count++)
            {
                Object* created = Allocate(engine, fixture.mutator, node);
                if (created == nullptr)
                {
                    return;
                }
                // A new object must not sit on the memory of an object that the model still reaches.
                for (size_t index = 0; index < object.size(); index++)
                {
                    if (object[index] == created)
                    {
                        Check(false, "F8 stress: a new allocation took the address of a reachable object");
                        return;
                    }
                }
                int index = static_cast<int>(object.size());
                object.push_back(created);
                field.push_back({-1, -1});
                int parent = pick(live);
                if (!Check(link(parent, random() % 2, index), "F8 stress newborn links"))
                {
                    return;
                }
                if (random() % 2 == 0 && !Check(link(index, random() % 2, pick(live)), "F8 stress newborn points back"))
                {
                    return;
                }
                live.push_back(index);
            }
            size_t changes = 20 + random() % 40;
            for (size_t count = 0; count < changes; count++)
            {
                int source = pick(live);
                int target = random() % 4 == 0 ? -1 : pick(live);
                if (!Check(link(source, random() % 2, target), "F8 stress store"))
                {
                    return;
                }
            }
            std::vector<bool> seen = reach();
            std::vector<int> reachable = liveList(seen);
            root.clear();
            size_t roots = 1 + random() % 5;
            for (size_t count = 0; count < roots; count++)
            {
                root.push_back(pick(reachable));
            }
            seen = reach();
            for (size_t index = 0; index < object.size(); index++)
            {
                if (!seen[index])
                {
                    object[index] = nullptr;
                    field[index] = {-1, -1};
                }
            }
            fixture.roots.objects.clear();
            for (int index : root)
            {
                fixture.roots.objects.push_back(object[index]);
            }
            LxrTracePhase phase = engine.GetTracePhase();
            uint32_t choice = random() % 10;
            LxrPauseKind kind = LxrPauseKind::RefCount;
            if (phase == LxrTracePhase::Inactive)
            {
                kind = choice < 5 ? LxrPauseKind::RefCount : choice < 9 ? LxrPauseKind::InitialMark : LxrPauseKind::Full;
            }
            else if (phase == LxrTracePhase::Concurrent)
            {
                kind = choice < 4 ? LxrPauseKind::RefCount : LxrPauseKind::FinalMark;
            }
            traces += kind == LxrPauseKind::InitialMark ? 1 : 0;
            cycleSweeps += kind == LxrPauseKind::FinalMark || kind == LxrPauseKind::Full ? 1 : 0;
            LxrReferenceCounting& pauseEngine = engine;
            LxrRcResult paused = kind == LxrPauseKind::InitialMark
                ? pauseEngine.BeginTrace(CaptureMoving, &fixture.roots, Timeout)
                : kind == LxrPauseKind::FinalMark
                    ? pauseEngine.FinishTrace(CaptureMoving, &fixture.roots, Timeout)
                    : kind == LxrPauseKind::Full
                        ? pauseEngine.CollectFull(CaptureMoving, &fixture.roots, Timeout)
                        : pauseEngine.Collect(CaptureMoving, &fixture.roots, Timeout);
            if (!Check(paused == LxrRcResult::Success, "F8 pause completes"))
            {
                std::fprintf(stderr, "F8 pause failure=%u\n", static_cast<unsigned>(engine.GetFailure()));
                return;
            }
#if defined(LXR_F8_WAIT_CONTROL)
            // Control: the behaviour before F8. The thread waits for the lazy work after the pause.
            if (kind != LxrPauseKind::InitialMark && !Check(engine.Drain(Timeout) == LxrRcResult::Success, "F8 control drains"))
            {
                return;
            }
#endif // LXR_F8_WAIT_CONTROL
            if (nursery)
            {
                // The pause may have moved young objects. Follow the real graph from the moved roots.
                std::vector<bool> done(object.size(), false);
                std::vector<int> stack;
                for (size_t index = 0; index < root.size(); index++)
                {
                    object[root[index]] = fixture.roots.objects[index];
                    stack.push_back(root[index]);
                }
                while (!stack.empty())
                {
                    int index = stack.back();
                    stack.pop_back();
                    if (done[index])
                    {
                        continue;
                    }
                    done[index] = true;
                    for (size_t slot = 0; slot < 2; slot++)
                    {
                        int target = field[index][slot];
                        if (target >= 0)
                        {
                            object[target] = *Field(object[index], slot);
                            stack.push_back(target);
                        }
                    }
                }
            }
            size_t wrong = 0;
            for (size_t index = 0; index < object.size(); index++)
            {
                if (object[index] == nullptr)
                {
                    continue;
                }
                bool ok = Count(engine, object[index]) != 0 && engine.GetObjectExtent(object[index]) == 32 &&
                    engine.IsObjectStart(object[index]);
                for (size_t slot = 0; slot < 2; slot++)
                {
                    int target = field[index][slot];
                    ok = ok && *Field(object[index], slot) == (target < 0 ? nullptr : object[target]);
                }
                wrong += ok ? 0 : 1;
            }
            if (!Check(wrong == 0, "F8 stress: every reachable object is counted, owned and linked while lazy work runs"))
            {
                std::fprintf(stderr, "seed %u round %zu kind %u wrong %zu\n", seed, round,
                    static_cast<unsigned>(kind), wrong);
                return;
            }
        }
        // End: one Full pause with the roots, a drain, and the oracle has to agree with the model.
        std::vector<bool> seen = reach();
        size_t reachableCount = 0;
        for (size_t index = 0; index < seen.size(); index++)
        {
            reachableCount += seen[index] ? 1 : 0;
        }
        fixture.roots.objects.clear();
        for (int index : root)
        {
            fixture.roots.objects.push_back(object[index]);
        }
        if (engine.GetTracePhase() == LxrTracePhase::Concurrent &&
            !Check(engine.FinishTrace(CaptureMoving, &fixture.roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "F8 the last trace ends"))
        {
            return;
        }
        if (!Check(engine.Drain(Timeout) == LxrRcResult::Success &&
                engine.CollectFull(CaptureMoving, &fixture.roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "F8 the final Full pause and drain complete"))
        {
            return;
        }
        std::printf("f8_stress seed=%u nursery=%d overlap_rounds=%zu initial_marks=%zu cycle_sweeps=%zu live=%zu reachable=%zu\n",
            seed, nursery ? 1 : 0, overlapRounds, traces, cycleSweeps,
            static_cast<size_t>(engine.GetLiveBytes() / 32), reachableCount);
        Check(engine.GetLiveBytes() == reachableCount * 32,
            "F8 stress: after the drain exactly the reachable objects are alive (no leak, no early free)");
        Check(overlapRounds >= 15, "F8 stress: the program ran in many rounds while lazy work was still pending");
        Check(traces >= 2 && cycleSweeps >= 2, "F8 stress runs trace cycles and cycle sweeps while lazy work is pending");
        Check(engine.GetFailure() == LxrRcResult::Success, "F8 stress: the engine did not fault");
        Check(engine.DetachMutator(fixture.mutator) == LxrRcResult::Success, "F8 stress fixture detaches");
    }

    // P25 (F5). The emergency rule needs the pages that hold data. Where survivors stay in place, the blocks of a heap that
    // holds little live data are all non-free, and a count of blocks says "full". The occupied figure (blocks minus the free
    // lines of the reusable blocks) says how much data there is. For a dense heap both figures agree, so a heap that really
    // is full still gets its emergency pause.
    void ValidateF5OccupiedPages(bool dense)
    {
        Type link(24, 1);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        constexpr size_t Total = 5400;
        Object* head = nullptr;
        Object* tail = nullptr;
        for (size_t index = 0; index < Total; index++)
        {
            Object* object = Allocate(engine, mutator, link);
            if (object == nullptr)
            {
                return;
            }
            if (dense || index % 100 == 0)
            {
                if (tail != nullptr && !Check(Store(engine, mutator, Field(tail), object), "F5 survivor chain links"))
                {
                    return;
                }
                head = head == nullptr ? object : head;
                tail = object;
            }
        }
        if (!Epoch(engine, {head}))
        {
            return;
        }
        size_t pages = 0;
        size_t occupied = 0;
        if (!Check(engine.GetMaturePages(&pages) == LxrRcResult::Success &&
                engine.GetOccupiedPages(&occupied) == LxrRcResult::Success, "F5 page figures are readable"))
        {
            return;
        }
        size_t total = dense ? occupied + occupied / 10 : pages + pages / 10;
        LxrCollectionPolicy blockPolicy;
        LxrCollectionPolicy occupiedPolicy;
        Check(blockPolicy.FinishLazySweep(LxrPauseKind::RefCount, total, pages, false, true, true) &&
                occupiedPolicy.FinishLazySweep(LxrPauseKind::RefCount, total, occupied, false, true, true),
            "F5 policy accepts both figures");
        std::printf("f5_pages dense=%d blocks=%zu block_pages=%zu occupied_pages=%zu total_pages=%zu\n",
            dense ? 1 : 0, engine.GetNonFreeBlocks(), pages, occupied, total);
        Check(occupied <= pages, "F5 occupied pages never exceed block pages");
        if (dense)
        {
            Check(blockPolicy.EmergencyHint() && occupiedPolicy.EmergencyHint(),
                "a heap that holds that much data is full by both figures and gets its emergency pause");
        }
        else
        {
            Check(engine.GetNonFreeBlocks() >= 3 && occupied * 4 < pages,
                "survivors that stay in place hold a fraction of the pages of their blocks");
            Check(blockPolicy.EmergencyHint(), "the count of whole blocks calls the sparse heap full");
            Check(!occupiedPolicy.EmergencyHint(), "the count of occupied pages does not call a sparse heap full");
        }
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "F5 mutator detaches");
    }

    // P25 (F4). The number of workers follows the Java LXR rule (ParallelGCThreads, ConcGCThreads). The table is the rule of
    // OpenJDK: one worker per processor up to 8, then 5 for every 8 more; the concurrent workers are (workers + 2) / 4.
    void ValidateF4WorkerPolicy()
    {
        struct Row
        {
            size_t processors;
            size_t parallel;
            size_t concurrent;
        };
        constexpr Row Rows[] = {
            {1, 1, 1}, {2, 2, 1}, {4, 4, 1}, {8, 8, 2}, {9, 8, 2}, {12, 10, 3}, {16, 13, 3}, {24, 18, 5},
            {32, 23, 6}, {64, 43, 11}, {256, 64, 16},
        };
        size_t wrong = 0;
        for (const Row& row : Rows)
        {
            size_t parallel = LxrWorkerPolicy::ParallelThreads(row.processors);
            size_t concurrent = LxrWorkerPolicy::ConcurrentThreads(parallel);
            if (parallel != row.parallel || concurrent != row.concurrent)
            {
                std::fprintf(stderr, "processors %zu: %zu/%zu, expected %zu/%zu\n", row.processors, parallel,
                    concurrent, row.parallel, row.concurrent);
                wrong++;
            }
        }
        Check(wrong == 0, "the worker counts follow the Java LXR rule (16 processors: 13 workers, 3 concurrent)");
    }

    struct F4Sleeper
    {
        std::atomic<int> running{0};
        std::atomic<int> peak{0};
        std::atomic<int> done{0};
        int sleepMilliseconds = 0;
    };

    void F4Run(LxrWorkPacket*, size_t, void* context)
    {
        F4Sleeper* state = static_cast<F4Sleeper*>(context);
        int now = state->running.fetch_add(1) + 1;
        int seen = state->peak.load();
        while (now > seen && !state->peak.compare_exchange_weak(seen, now))
        {
        }
        if (state->sleepMilliseconds != 0)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(state->sleepMilliseconds));
        }
        state->running.fetch_sub(1);
        state->done.fetch_add(1);
    }

    // A packet that is submitted wakes at most one worker, and a packet that completes wakes none. The count of wake-up calls
    // (metrics.signals) is bounded by the number of packets. The old scheduler woke everybody twice for each packet.
    void ValidateF4Wakeups()
    {
        constexpr size_t Packets = 2000;
        LxrWorkScheduler scheduler;
        if (!Check(scheduler.Initialize(8) == LxrWorkSchedulerResult::Success, "F4 scheduler starts 8 workers"))
        {
            return;
        }
        F4Sleeper state;
        std::vector<std::unique_ptr<LxrWorkPacket>> packets;
        for (size_t index = 0; index < Packets; index++)
        {
            packets.push_back(std::make_unique<LxrWorkPacket>(
                index + 1, LxrWorkBucketStage::Unconstrained, nullptr, 0, F4Run, &state));
            if (!Check(scheduler.Submit(packets.back().get()) == LxrWorkSchedulerResult::Success, "F4 packet submits"))
            {
                return;
            }
        }
        Check(scheduler.WaitForBucketIdle(LxrWorkBucketStage::Unconstrained, Timeout) == LxrWorkSchedulerResult::Success &&
                state.done.load() == static_cast<int>(Packets), "every packet runs once");
        LxrWorkSchedulerMetrics metrics = scheduler.GetMetrics();
        std::printf("f4_wakeups packets=%zu signals=%llu parks=%llu wakes=%llu\n", Packets,
            static_cast<unsigned long long>(metrics.signals), static_cast<unsigned long long>(metrics.parks),
            static_cast<unsigned long long>(metrics.wakes));
        Check(metrics.executed == Packets && metrics.ownershipViolations == 0, "F4 scheduler accounting is exact");
        Check(metrics.signals <= Packets + 64,
            "the scheduler wakes at most one worker for a packet and nobody when a packet completes");
        Check(scheduler.Shutdown() == LxrWorkSchedulerResult::Success, "F4 scheduler stops");
    }

    // At most ConcGCThreads workers run the packets of the concurrent trace while the program runs. The other packets use
    // every worker.
    void ValidateF4ConcurrentLimit()
    {
        LxrWorkScheduler scheduler;
        if (!Check(scheduler.Initialize(6) == LxrWorkSchedulerResult::Success &&
                scheduler.ConfigureConcurrentTraceWorkers(2) == LxrWorkSchedulerResult::Success &&
                scheduler.ConfigureConcurrentTraceWorkers(7) == LxrWorkSchedulerResult::InvalidArgument &&
                scheduler.EnableConcurrentStage(LxrWorkBucketStage::ConcurrentResumable) == LxrWorkSchedulerResult::Success,
                "F4 scheduler with a limit of 2 concurrent trace workers starts"))
        {
            return;
        }
        for (LxrWorkBucketStage stage : {LxrWorkBucketStage::ConcurrentResumable, LxrWorkBucketStage::Unconstrained})
        {
            F4Sleeper state;
            state.sleepMilliseconds = 30;
            std::vector<std::unique_ptr<LxrWorkPacket>> packets;
            for (size_t index = 0; index < 12; index++)
            {
                packets.push_back(std::make_unique<LxrWorkPacket>(index + 1, stage, nullptr, 0, F4Run, &state));
                if (!Check(scheduler.Submit(packets.back().get()) == LxrWorkSchedulerResult::Success, "F4 packet submits"))
                {
                    return;
                }
            }
            Check(scheduler.WaitForBucketIdle(stage, Timeout) == LxrWorkSchedulerResult::Success &&
                    state.done.load() == 12, "all packets of the stage run");
            std::printf("f4_limit stage=%s peak=%d\n", LxrGetWorkBucketStageName(stage), state.peak.load());
            if (stage == LxrWorkBucketStage::Unconstrained)
            {
                Check(state.peak.load() >= 3, "packets of other stages use more than the concurrent trace limit");
            }
            else
            {
                Check(state.peak.load() >= 1 && state.peak.load() <= 2,
                    "no more than 2 workers run concurrent trace packets at once");
            }
        }
        Check(scheduler.Shutdown() == LxrWorkSchedulerResult::Success, "F4 limit scheduler stops");
    }

    // The increments and the decrements of a chain run in one packet, not in one packet for each level.
    void ValidateF4Recursion()
    {
        constexpr size_t Chain = 500;
        Type link(24, 1);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity) ||
            !Check(engine.EnableStorageDiagnostics(Timeout) == LxrRcResult::Success, "F4 storage accounting starts"))
        {
            return;
        }
        std::vector<Object*> nodes;
        for (size_t index = 0; index < Chain; index++)
        {
            Object* node = Allocate(engine, mutator, link);
            if (node == nullptr)
            {
                return;
            }
            if (!nodes.empty() && !Check(Store(engine, mutator, Field(nodes.back()), node), "F4 chain link stores"))
            {
                return;
            }
            nodes.push_back(node);
        }
        uint64_t before = engine.GetStorageMetric(0);
        if (!Epoch(engine, {nodes[0]}))
        {
            return;
        }
        uint64_t created = engine.GetStorageMetric(0) - before;
        size_t counted = 0;
        for (Object* node : nodes)
        {
            counted += Count(engine, node) != 0 ? 1 : 0;
        }
        std::printf("f4_recursion chain=%zu increment_pause_packets=%llu\n", Chain, static_cast<unsigned long long>(created));
        Check(counted == Chain && created <= 12,
            "the increments of a chain of young objects run in one packet (every node is counted)");
        before = engine.GetStorageMetric(0);
        if (!Epoch(engine, {}) || !Epoch(engine, {}))
        {
            return;
        }
        created = engine.GetStorageMetric(0) - before;
        std::printf("f4_recursion chain=%zu decrement_pause_packets=%llu\n", Chain, static_cast<unsigned long long>(created));
        Check(engine.GetLiveBytes() == 0 && created <= 24,
            "the decrements of a dead chain run in one packet (every node is reclaimed)");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "F4 recursion mutator detaches");
    }

    // P26 (F2). The cost of a pause does not follow the trace backlog. One worker may run trace packets and a gate blocks it
    // after its first packet, so the packets that the test queues stay in the scheduler. A RefCount pause then runs with
    // 500 and with 8 000 pending packets. The walks of the pause (Retire, FixForwardingReaders, PublishTraceWork) must visit
    // a number of packets that does not depend on the backlog. Control LXR_F2_WALK_BACKLOG_CONTROL: the packets are in the
    // list that the pause walks again.
    bool InitializeF2(F1Fixture& fixture, bool nursery)
    {
        LxrRcConfiguration configuration{
            64 * ImmixBlockGeometry::BlockBytes, 8 * LxrLargeObjectSpace::MinimumObjectBytes, 4, 2048, 4, 8,
        };
        configuration.evacuateNursery = nursery;
        configuration.concurrentTraceWorkers = 1;
        fixture.model.context = &fixture.roots;
        fixture.model.finishRelocation = RelocateMoving;
        LxrPauseRuntimeAdapter runtime{Suspend, Resume, nullptr, false};
        return Check(fixture.engine.Initialize(configuration, runtime, &fixture.model) == LxrRcResult::Success &&
                fixture.engine.AttachMutator(&fixture.identity, &fixture.mutator) == LxrRcResult::Success,
            "F2 fixture engine and mutator initialize");
    }

    void ValidateF2Backlog(bool nursery, size_t backlog)
    {
        F1Fixture fixture;
        if (!InitializeF2(fixture, nursery))
        {
            return;
        }
        LxrReferenceCounting& engine = fixture.engine;
        Type link(24, 1);
        Type leaf(24, 0);
        Object* anchor = Allocate(engine, fixture.mutator, link);
        Object* child = Allocate(engine, fixture.mutator, leaf);
        if (anchor == nullptr || child == nullptr ||
            !Check(Store(engine, fixture.mutator, Field(anchor), child), "F2 anchor points to a child"))
        {
            return;
        }
        fixture.roots.objects = {anchor};
        constexpr size_t Holders = 200;
        for (size_t index = 0; index < Holders; index++)
        {
            Object* holder = Allocate(engine, fixture.mutator, link);
            Object* held = Allocate(engine, fixture.mutator, leaf);
            if (holder == nullptr || held == nullptr ||
                !Check(Store(engine, fixture.mutator, Field(holder), held), "F2 holder points to a first target"))
            {
                return;
            }
            fixture.roots.objects.push_back(holder);
        }
        if (!F1Pause(fixture, LxrPauseKind::RefCount))
        {
            return;
        }
        engine.ArmTraceGateForTest(1);
        if (!F1Pause(fixture, LxrPauseKind::InitialMark))
        {
            return;
        }
        std::chrono::steady_clock::time_point deadline = std::chrono::steady_clock::now() +
            std::chrono::milliseconds(Timeout);
        while (!engine.IsTraceGateBlockedForTest() && std::chrono::steady_clock::now() < deadline)
        {
            std::this_thread::yield();
        }
        anchor = fixture.roots.objects[0];
        child = *Field(anchor);
        // The program overwrites the field of every mature holder: the first write of a slot logs the old value, so the
        // decrement log of the next pause has one entry for each holder.
        Object* replacement = Allocate(engine, fixture.mutator, leaf);
        bool stored = replacement != nullptr;
        for (size_t index = 1; stored && index <= Holders; index++)
        {
            stored = Store(engine, fixture.mutator, Field(fixture.roots.objects[index]), replacement);
        }
        if (!Check(stored && engine.IsTraceGateBlockedForTest() && child != nullptr &&
                engine.QueueTraceBacklogForTest(child, backlog) == LxrRcResult::Success,
                "F2 the only trace worker is blocked and the backlog is queued"))
        {
            return;
        }
        uint64_t pending = engine.GetTracePendingForTest();
        Check(pending >= backlog, "F2 the backlog is pending in the scheduler");
        if (!F1Pause(fixture, LxrPauseKind::RefCount))
        {
            return;
        }
        LxrPauseWalkMetrics walk = engine.GetPauseWalkMetrics();
        std::printf("f2_backlog nursery=%d pending=%llu retire=%llu fix_packets=%llu fix_log=%llu publish=%llu rewritten=%llu\n",
            nursery ? 1 : 0, static_cast<unsigned long long>(pending), static_cast<unsigned long long>(walk.retireVisited),
            static_cast<unsigned long long>(walk.fixPackets), static_cast<unsigned long long>(walk.fixLogEntries),
            static_cast<unsigned long long>(walk.publishVisited), static_cast<unsigned long long>(walk.fixRewritten));
        Check(walk.retireVisited <= 96 && walk.fixPackets <= 96 && walk.publishVisited <= 96,
            "F2 the packets that a pause visits do not follow the number of pending trace packets");
#ifndef _DEBUG
        // A Debug build walks the log to check it (the Java LXR never walks it); a Release build must not.
        Check(walk.fixLogEntries == 0, "F2 a pause that copies only young objects does not walk the decrement log");
#endif // !_DEBUG
        deadline = std::chrono::steady_clock::now() + std::chrono::milliseconds(Timeout);
        while (!engine.IsTraceExhausted() && std::chrono::steady_clock::now() < deadline)
        {
            std::this_thread::yield();
        }
        Check(engine.IsTraceExhausted(), "F2 the backlog runs to the end after the pause");
        if (!F1Pause(fixture, LxrPauseKind::FinalMark))
        {
            return;
        }
        anchor = fixture.roots.objects[0];
        child = *Field(anchor);
        Check(engine.GetFailure() == LxrRcResult::Success && child != nullptr && engine.IsMarked(child) &&
                Count(engine, child) != 0,
            "F2 the trace ends with the child marked and counted");
        Check(engine.DetachMutator(fixture.mutator) == LxrRcResult::Success, "F2 fixture detaches");
    }

    // P26 (F2). A shutdown with a backlog that nobody has run frees every pending trace packet (no list owns them).
    void ValidateF2ShutdownWithBacklog()
    {
        F1Fixture fixture;
        if (!InitializeF2(fixture, false))
        {
            return;
        }
        LxrReferenceCounting& engine = fixture.engine;
        Type link(24, 1);
        Type leaf(24, 0);
        Object* anchor = Allocate(engine, fixture.mutator, link);
        Object* child = Allocate(engine, fixture.mutator, leaf);
        if (anchor == nullptr || child == nullptr ||
            !Check(Store(engine, fixture.mutator, Field(anchor), child), "F2 shutdown anchor points to a child"))
        {
            return;
        }
        fixture.roots.objects = {anchor};
        if (!F1Pause(fixture, LxrPauseKind::RefCount))
        {
            return;
        }
        engine.ArmTraceGateForTest(1);
        if (!F1Pause(fixture, LxrPauseKind::InitialMark))
        {
            return;
        }
        std::chrono::steady_clock::time_point deadline = std::chrono::steady_clock::now() +
            std::chrono::milliseconds(Timeout);
        while (!engine.IsTraceGateBlockedForTest() && std::chrono::steady_clock::now() < deadline)
        {
            std::this_thread::yield();
        }
        child = *Field(fixture.roots.objects[0]);
        Check(engine.IsTraceGateBlockedForTest() && engine.QueueTraceBacklogForTest(child, 2000) == LxrRcResult::Success &&
                engine.GetTracePendingForTest() >= 2000,
            "F2 a backlog is pending when the engine shuts down");
        // The gate is still closed. The destructor of the fixture shuts the engine down; the blocked worker is released by
        // the phase change of Shutdown. The run ends without a crash and without a leak report.
        engine.StopWorkers();
    }

    // A glance at the trace limit of the collector: the Java rule, with a floor of 2 trace workers on small machines.
    void ValidateF4bTraceLimitDefault()
    {
        struct Row { size_t workers; size_t java; size_t expected; };
        constexpr Row Rows[] = {{1, 1, 1}, {2, 1, 2}, {4, 1, 2}, {6, 2, 2}, {8, 2, 2}, {9, 2, 2}, {13, 3, 3}, {26, 7, 7}, {64, 16, 16}};
        for (const Row& row : Rows)
        {
            Check(LxrWorkerPolicy::ConcurrentThreads(row.workers) == row.java,
                "F4b the Java rule for the concurrent trace workers (ParallelGCThreads + 2) / 4");
            Check(LxrWorkerPolicy::DefaultConcurrentTraceThreads(row.workers) == row.expected,
                "F4b the default trace limit is the Java rule with a floor of 2 workers on a machine with 2 or more");
        }
    }

    // P26 (F4b). The trace packet loops over its children (the Java LXR trace loop, cm.rs: do_work). A chain of objects is
    // traced by a few packets, not by one packet for each object. Control LXR_F4B_NO_TRACE_LOOP_CONTROL: the packet ends
    // after its own objects.
    void ValidateF4bTraceLoop()
    {
        constexpr size_t Chain = 2000;
        Type link(24, 1);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity) ||
            !Check(engine.EnableStorageDiagnostics(Timeout) == LxrRcResult::Success, "F4b storage accounting starts"))
        {
            return;
        }
        std::vector<Object*> nodes;
        for (size_t index = 0; index < Chain; index++)
        {
            Object* node = Allocate(engine, mutator, link);
            if (node == nullptr)
            {
                return;
            }
            if (!nodes.empty() && !Check(Store(engine, mutator, Field(nodes.back()), node), "F4b chain link stores"))
            {
                return;
            }
            nodes.push_back(node);
        }
        std::vector<Object*> held{nodes[0]};
        Roots roots{&held};
        if (!Check(Epoch(engine, held), "F4b chain matures"))
        {
            return;
        }
        uint64_t before = engine.GetStorageMetric(0);
        if (!Check(engine.BeginTrace(Capture, &roots, Timeout) == LxrRcResult::Success, "F4b trace starts"))
        {
            return;
        }
        std::chrono::steady_clock::time_point deadline = std::chrono::steady_clock::now() +
            std::chrono::milliseconds(Timeout);
        while (!engine.IsTraceExhausted() && std::chrono::steady_clock::now() < deadline)
        {
            std::this_thread::yield();
        }
        uint64_t created = engine.GetStorageMetric(0) - before;
        std::printf("f4b_trace_loop chain=%zu packets=%llu\n", Chain, static_cast<unsigned long long>(created));
        Check(engine.IsTraceExhausted() && created < 100,
            "a chain of 2000 objects is traced by a few packets (the packet loops over the children it finds)");
        size_t marked = 0;
        for (Object* node : nodes)
        {
            marked += engine.IsMarked(node) ? 1 : 0;
        }
        Check(marked == Chain, "every object of the chain is marked");
        Check(engine.FinishTrace(Capture, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success && engine.GetFailure() == LxrRcResult::Success,
            "F4b the trace ends");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "F4b trace loop mutator detaches");
    }

    // P26 (F4b). Work stealing. One packet runs on a worker and submits 20 000 child packets from there; they go to the deque
    // of that worker. The other workers have to take them. Every packet runs exactly once. Control
    // LXR_F4B_NO_STEAL_CONTROL: a worker never steals, so one worker runs everything.
    struct F4bSteal
    {
        LxrWorkScheduler* scheduler = nullptr;
        std::vector<std::unique_ptr<LxrWorkPacket>> children;
        std::vector<std::atomic<int>> executions;
        std::vector<std::atomic<int>> byWorker;
        std::atomic<int> submitFailures{0};
        std::atomic<uint64_t> sink{0};
        F4bSteal(size_t count, size_t workers) : executions(count), byWorker(workers + 1)
        {
            for (std::atomic<int>& value : executions)
            {
                value = 0;
            }
            for (std::atomic<int>& value : byWorker)
            {
                value = 0;
            }
        }
    };

    void F4bChildRun(LxrWorkPacket* packet, size_t worker, void* context)
    {
        F4bSteal* state = static_cast<F4bSteal*>(context);
        state->executions[packet->GetId() - 1].fetch_add(1);
        state->byWorker[worker].fetch_add(1);
        uint64_t value = packet->GetId();
        for (int round = 0; round < 400; round++)
        {
            value = value * 6364136223846793005ULL + 1442695040888963407ULL;
        }
        state->sink.fetch_add(value, std::memory_order_relaxed);
    }

    void F4bParentRun(LxrWorkPacket*, size_t, void* context)
    {
        F4bSteal* state = static_cast<F4bSteal*>(context);
        for (std::unique_ptr<LxrWorkPacket>& child : state->children)
        {
            if (state->scheduler->Submit(child.get()) != LxrWorkSchedulerResult::Success)
            {
                state->submitFailures.fetch_add(1);
            }
        }
    }

    void ValidateF4bStealing()
    {
        constexpr size_t Children = 20000;
        constexpr size_t Workers = 8;
        LxrWorkScheduler scheduler;
        F4bSteal state(Children, Workers);
        state.scheduler = &scheduler;
        if (!Check(scheduler.Initialize(Workers) == LxrWorkSchedulerResult::Success, "F4b stealing scheduler starts"))
        {
            return;
        }
        for (size_t index = 0; index < Children; index++)
        {
            state.children.push_back(std::make_unique<LxrWorkPacket>(
                index + 1, LxrWorkBucketStage::Unconstrained, nullptr, 0, F4bChildRun, &state));
        }
        LxrWorkPacket parent(Children + 1, LxrWorkBucketStage::Unconstrained, nullptr, 0, F4bParentRun, &state);
        Check(scheduler.Submit(&parent) == LxrWorkSchedulerResult::Success &&
                scheduler.WaitForIdle(Timeout) == LxrWorkSchedulerResult::Success,
            "F4b the parent and its children run");
        size_t exactlyOnce = 0;
        for (std::atomic<int>& value : state.executions)
        {
            exactlyOnce += value.load() == 1 ? 1 : 0;
        }
        size_t workersUsed = 0;
        int busiest = 0;
        for (size_t worker = 0; worker < Workers; worker++)
        {
            int ran = state.byWorker[worker].load();
            workersUsed += ran != 0 ? 1 : 0;
            busiest = ran > busiest ? ran : busiest;
        }
        LxrWorkSchedulerMetrics metrics = scheduler.GetMetrics();
        std::printf("f4b_steal children=%zu workers_used=%zu busiest=%d executed=%llu\n", Children, workersUsed, busiest,
            static_cast<unsigned long long>(metrics.executed));
        Check(state.submitFailures.load() == 0 && exactlyOnce == Children, "F4b every child runs exactly once");
        Check(metrics.executed == Children + 1 && metrics.ownershipViolations == 0, "F4b the counters agree");
        Check(workersUsed >= 4 && busiest < static_cast<int>(Children * 3 / 4),
            "F4b work that lands in one worker is taken by the others (stealing)");
        Check(scheduler.Shutdown() == LxrWorkSchedulerResult::Success, "F4b stealing scheduler stops");
    }

    // P26 (F4b). No packet is lost and none runs twice. Four threads submit packets while eight workers run them and make
    // more. Control LXR_F4B_STEAL_NO_CAS_CONTROL: a thief takes a packet without the compare-and-swap that claims it.
    struct F4bStress
    {
        LxrWorkScheduler* scheduler = nullptr;
        std::vector<std::unique_ptr<LxrWorkPacket>> packets;
        std::vector<std::atomic<int>> executions;
        std::atomic<size_t> next{0};
        std::atomic<int> failures{0};
        explicit F4bStress(size_t count) : executions(count)
        {
            for (std::atomic<int>& value : executions)
            {
                value = 0;
            }
        }
    };

    void F4bStressRun(LxrWorkPacket* packet, size_t, void* context)
    {
        F4bStress* state = static_cast<F4bStress*>(context);
        size_t id = packet->GetId() - 1;
        state->executions[id].fetch_add(1);
        // A packet makes up to two more, as long as the pool lasts.
        for (int copy = 0; copy < static_cast<int>(id % 3); copy++)
        {
            size_t index = state->next.fetch_add(1);
            if (index < state->packets.size())
            {
                if (state->scheduler->Submit(state->packets[index].get()) != LxrWorkSchedulerResult::Success)
                {
                    state->failures.fetch_add(1);
                }
            }
        }
    }

    void ValidateF4bStress()
    {
        constexpr size_t Total = 60000;
        constexpr size_t Seeds = 4000;
        LxrWorkScheduler scheduler;
        F4bStress state(Total);
        state.scheduler = &scheduler;
        if (!Check(scheduler.Initialize(8) == LxrWorkSchedulerResult::Success, "F4b stress scheduler starts"))
        {
            return;
        }
        for (size_t index = 0; index < Total; index++)
        {
            state.packets.push_back(std::make_unique<LxrWorkPacket>(
                index + 1, LxrWorkBucketStage::Unconstrained, nullptr, 0, F4bStressRun, &state));
        }
        state.next.store(Seeds);
        std::vector<std::thread> threads;
        for (size_t thread = 0; thread < 4; thread++)
        {
            threads.emplace_back([&, thread]()
            {
                for (size_t index = thread; index < Seeds; index += 4)
                {
                    if (scheduler.Submit(state.packets[index].get()) != LxrWorkSchedulerResult::Success)
                    {
                        state.failures.fetch_add(1);
                    }
                }
            });
        }
        for (std::thread& thread : threads)
        {
            thread.join();
        }
        Check(scheduler.WaitForIdle(Timeout) == LxrWorkSchedulerResult::Success, "F4b the stress work drains");
        size_t submitted = state.next.load() < Total ? state.next.load() : Total;
        size_t once = 0;
        size_t twice = 0;
        size_t never = 0;
        for (size_t index = 0; index < submitted; index++)
        {
            int ran = state.executions[index].load();
            once += ran == 1 ? 1 : 0;
            twice += ran > 1 ? 1 : 0;
            never += ran == 0 ? 1 : 0;
        }
        LxrWorkSchedulerMetrics metrics = scheduler.GetMetrics();
        std::printf("f4b_stress submitted=%zu once=%zu twice=%zu never=%zu executed=%llu\n", submitted, once, twice,
            never, static_cast<unsigned long long>(metrics.executed));
        Check(state.failures.load() == 0 && twice == 0 && never == 0 && once == submitted,
            "F4b no packet is lost and none runs twice");
        Check(metrics.submitted == submitted && metrics.executed == submitted && metrics.ownershipViolations == 0,
            "F4b the counters agree with the packets");
        Check(scheduler.Shutdown() == LxrWorkSchedulerResult::Success, "F4b stress scheduler stops");
    }

    // P26 (F4b). The concurrent trace stage closes for a pause without moving its packets. Packets of the stage that a worker
    // made stay in its deque, a pause runs them itself (RunQueuedInline) and a later Resume lets the workers take the rest.
    void ValidateF4bPauseKeepsTracePackets()
    {
        constexpr size_t Count = 3000;
        LxrWorkScheduler scheduler;
        F4bSteal state(Count, 4);
        state.scheduler = &scheduler;
        if (!Check(scheduler.Initialize(4) == LxrWorkSchedulerResult::Success,
                "F4b scheduler with a closed trace stage starts"))
        {
            return;
        }
        for (size_t index = 0; index < Count; index++)
        {
            state.children.push_back(std::make_unique<LxrWorkPacket>(
                index + 1, LxrWorkBucketStage::ConcurrentResumable, nullptr, 0, F4bChildRun, &state));
        }
        // The trace stage is not open, so the packets stay queued.
        for (std::unique_ptr<LxrWorkPacket>& child : state.children)
        {
            scheduler.Submit(child.get());
        }
        uint64_t ran = 0;
        size_t before = 0;
        for (std::atomic<int>& value : state.executions)
        {
            before += value.load();
        }
        Check(before == 0 && scheduler.GetStageOutstanding(LxrWorkBucketStage::ConcurrentResumable) == Count,
            "F4b a closed stage runs nothing and keeps every packet");
        Check(scheduler.RunQueuedInline(LxrWorkBucketStage::ConcurrentResumable, 4, &ran) == LxrWorkSchedulerResult::Success &&
                ran == Count, "F4b a pause runs the queued packets of the closed stage itself");
        size_t once = 0;
        for (std::atomic<int>& value : state.executions)
        {
            once += value.load() == 1 ? 1 : 0;
        }
        Check(once == Count && scheduler.GetStageOutstanding(LxrWorkBucketStage::ConcurrentResumable) == 0,
            "F4b every queued packet ran exactly once and the stage is empty");
        Check(scheduler.Shutdown() == LxrWorkSchedulerResult::Success, "F4b pause scheduler stops");
    }

    // A large object that dies in a RefCount pause while a concurrent trace is active is released by the lazy
    // sweep after the pause. The large-object space is in its ConcurrentMark phase then, and the release must

    // be accepted (P17 fix). Before the fix the lazy sweep failed with MetadataFailure and the engine faulted.
    void ValidateLazyLargeReleaseDuringTrace()
    {
        Type equal(32768, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Object* large = Allocate(engine, mutator, equal);
        if (large == nullptr)
        {
            return;
        }
        std::vector<Object*> held{large};
        std::vector<Object*> none;
        Roots holdRoots{&held};
        Roots noRoots{&none};
        if (!Check(Epoch(engine, held) &&
                engine.HoldTrace(true) == LxrRcResult::Success &&
                engine.BeginTrace(Capture, &holdRoots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
                "a promoted large object is live when the concurrent trace starts"))
        {
            return;
        }
        Check(engine.GetObjectExtent(large) == equal.table->m_baseSize, "the large object has an extent before it dies");
        bool died = Check(engine.Collect(Capture, &noRoots, Timeout) == LxrRcResult::Success &&
            engine.Drain(Timeout) == LxrRcResult::Success,
            "a RefCount pause during the concurrent trace kills the large object and the lazy sweep succeeds");
        Check(engine.GetFailure() == LxrRcResult::Success, "the lazy sweep of a dead large object does not fault the engine");
        if (died)
        {
            Check(engine.GetMetrics().largeObjectBytesReleased >= 32768, "the dead large object is released during the concurrent trace");
            Check(engine.GetObjectExtent(large) == 0, "the released large object has no extent");
            Check(engine.HoldTrace(false) == LxrRcResult::Success &&
                engine.FinishTrace(Capture, &noRoots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "the trace finishes after the lazy release");
            Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "lazy-release owner detaches");
        }
    }

    // The inline accessors of the side-metadata fast path (P17 S2) must give the same location and value as
    // the checked SideMetadataManager functions, for every kind, every memory order and every operation
    // that the pause loops use. The test counts the mismatches of each operation and checks that the count is zero.
    class InlineMetadataComparison final
    {
    public:
        InlineMetadataComparison(SideMetadataManager& metadata, const LxrSideMetadataLayout& layout,
            LxrSideMetadataKind kind, uintptr_t start, size_t size)
            : m_metadata(metadata), m_kind(kind), m_start(start),
              m_accessor(metadata.GetAccessor(kind)),
              m_granularity(static_cast<size_t>(1) << layout.GetSpec(kind)->log_bytes_per_value),
              m_bits(static_cast<uint32_t>(1) << layout.GetSpec(kind)->log_bits_per_value),
              m_maximum(m_bits == 64 ? UINTPTR_MAX : (static_cast<uintptr_t>(1) << m_bits) - 1),
              m_entries(size / m_granularity), m_shadow(m_entries), m_state(0x9E3779B97F4A7C15ull + static_cast<uint64_t>(kind))
        {
        }

        void Run()
        {
            if (!Check(m_accessor.IsValid(), "inline accessor of the kind is valid"))
            {
                return;
            }
            size_t mismatches = 0;
            for (size_t entry = 0; entry < m_entries; entry++)
            {
                SideMetadataLocation checked;
                SideMetadataLocation inlined = m_accessor.Locate(Address(entry));
                bool ok = m_metadata.GetLocation(m_kind, Address(entry), &checked) == SideMetadataResult::Success &&
                    checked.word == inlined.word && checked.mask == inlined.mask &&
                    checked.shift == inlined.shift && checked.bit_count == inlined.bit_count;
                mismatches += ok ? 0 : 1;
            }
            Report(mismatches, "inline location equals the checked location");

            mismatches = 0;
            for (size_t entry = 0; entry < m_entries; entry++)
            {
                m_shadow[entry] = Next();
                mismatches += m_metadata.Store(m_kind, Address(entry), m_shadow[entry], Order(entry)) ==
                    SideMetadataResult::Success ? 0 : 1;
            }
            Report(mismatches, "checked stores fill the plane");
            CompareAll("inline load equals the checked load");

            mismatches = 0;
            for (size_t entry = 0; entry < m_entries; entry++)
            {
                m_shadow[entry] = Next();
                m_accessor.Store(Address(entry), m_shadow[entry], Order(entry + 1));
            }
            CompareAll("checked load sees the inline store, neighbors unchanged");

            mismatches = 0;
            for (size_t entry = 0; entry < m_entries; entry++)
            {
                uintptr_t replacement = Next();
                uintptr_t observed = 0;
                uintptr_t wrong = (m_shadow[entry] + 1) & m_maximum;
                bool exchanged = m_accessor.CompareExchange(Address(entry), replacement, wrong, Order(entry), &observed);
                mismatches += (exchanged || observed != m_shadow[entry]) ? 1 : 0;
                exchanged = m_accessor.CompareExchange(Address(entry), replacement, m_shadow[entry], Order(entry), &observed);
                mismatches += (!exchanged || observed != m_shadow[entry]) ? 1 : 0;
                m_shadow[entry] = replacement;
            }
            Report(mismatches, "inline compare-exchange reports the checked values");
            CompareAll("checked load sees the inline compare-exchange");

            mismatches = 0;
            for (size_t entry = 0; entry < m_entries; entry++)
            {
                uintptr_t addend = Next();
                uintptr_t previous = m_accessor.FetchAddWrapping(Address(entry), addend, Order(entry));
                mismatches += previous == m_shadow[entry] ? 0 : 1;
                m_shadow[entry] = (m_shadow[entry] + addend) & m_maximum;
            }
            Report(mismatches, "inline fetch-add returns the previous value");
            CompareAll("checked load sees the inline fetch-add");

            if (m_kind == LxrSideMetadataKind::ReferenceCount)
            {
                RunCounts();
            }
            RunRanges();
        }

    private:
        uintptr_t Address(size_t entry) const
        {
            return m_start + entry * m_granularity;
        }

        static SideMetadataMemoryOrder Order(size_t index)
        {
            return (index & 1) == 0 ? SideMetadataMemoryOrder::Relaxed : SideMetadataMemoryOrder::SequentiallyConsistent;
        }

        uintptr_t Next()
        {
            m_state ^= m_state << 13;
            m_state ^= m_state >> 7;
            m_state ^= m_state << 17;
            // Favor the edges of the value range so that wrapping and saturation happen.
            switch (m_state & 7)
            {
                case 0: return 0;
                case 1: return m_maximum;
                case 2: return (m_maximum - 1) & m_maximum;
                default: return static_cast<uintptr_t>(m_state >> 8) & m_maximum;
            }
        }

        void Report(size_t mismatches, const char* message) const
        {
            if (!Check(mismatches == 0, message))
            {
                std::fprintf(stderr, "  kind %u, %zu mismatches\n", static_cast<unsigned>(m_kind), mismatches);
            }
        }

        void CompareAll(const char* message)
        {
            size_t mismatches = 0;
            for (size_t entry = 0; entry < m_entries; entry++)
            {
                uintptr_t checked = 0;
                bool ok = m_metadata.Load(m_kind, Address(entry), SideMetadataMemoryOrder::Relaxed, &checked) ==
                    SideMetadataResult::Success && checked == m_shadow[entry] &&
                    m_accessor.Load(Address(entry), Order(entry)) == m_shadow[entry];
                mismatches += ok ? 0 : 1;
            }
            Report(mismatches, message);
        }

        void RunCounts()
        {
            size_t mismatches = 0;
            for (size_t entry = 0; entry < m_entries; entry++)
            {
                uintptr_t previous = 0;
                uintptr_t expected = m_shadow[entry];
                SideMetadataUpdateStatus status = m_accessor.IncrementSaturating(Address(entry), Order(entry), &previous);
                bool saturated = expected == m_maximum;
                mismatches += (previous != expected ||
                    status != (saturated ? SideMetadataUpdateStatus::Saturated : SideMetadataUpdateStatus::Updated)) ? 1 : 0;
                m_shadow[entry] = saturated ? expected : expected + 1;
            }
            Report(mismatches, "inline increment matches the checked increment rules");
            CompareAll("checked load sees the inline increment");

            mismatches = 0;
            for (size_t entry = 0; entry < m_entries; entry++)
            {
                uintptr_t previous = 0;
                uintptr_t expected = m_shadow[entry];
                SideMetadataUpdateStatus status = m_accessor.DecrementNonZeroNonSaturated(Address(entry), Order(entry), &previous);
                SideMetadataUpdateStatus wanted = expected == 0 ? SideMetadataUpdateStatus::Zero :
                    expected == m_maximum ? SideMetadataUpdateStatus::Saturated : SideMetadataUpdateStatus::Updated;
                mismatches += (previous != expected || status != wanted) ? 1 : 0;
                m_shadow[entry] = wanted == SideMetadataUpdateStatus::Updated ? expected - 1 : expected;
            }
            Report(mismatches, "inline decrement matches the checked decrement rules");
            CompareAll("checked load sees the inline decrement");
        }

        void RunRanges()
        {
            size_t mismatches = 0;
            if (m_metadata.ResetRangeQuiescent(m_kind, m_start, m_entries * m_granularity, 0) != SideMetadataResult::Success)
            {
                mismatches++;
            }
            std::fill(m_shadow.begin(), m_shadow.end(), 0);
            // Ranges of 1, 2, 3, 5, 31, 32, 33 and 129 entries, from every 7th starting entry.
            const size_t lengths[] = {1, 2, 3, 5, 31, 32, 33, 129};
            for (int pass = 0; pass < 2; pass++)
            {
                for (size_t first = 0; first < m_entries; first += 7)
                {
                    for (size_t length : lengths)
                    {
                        if (first + length > m_entries)
                        {
                            continue;
                        }
                        bool checkedZero = false;
                        bool ok = m_metadata.IsRangeZero(m_kind, Address(first), length * m_granularity,
                            Order(first), &checkedZero) == SideMetadataResult::Success &&
                            checkedZero == m_accessor.IsRangeZero(Address(first), length * m_granularity, Order(first));
                        mismatches += ok ? 0 : 1;
                    }
                }
                // Second pass: one set entry in the middle of the plane.
                if (pass == 0)
                {
                    size_t middle = m_entries / 2;
                    m_metadata.Store(m_kind, Address(middle), 1, SideMetadataMemoryOrder::Relaxed);
                    m_shadow[middle] = 1;
                }
            }
            Report(mismatches, "inline range check equals the checked range check");
        }

        SideMetadataManager& m_metadata;
        LxrSideMetadataKind m_kind;
        uintptr_t m_start;
        SideMetadataAccessor m_accessor;
        size_t m_granularity;
        uint32_t m_bits;
        uintptr_t m_maximum;
        size_t m_entries;
        std::vector<uintptr_t> m_shadow;
        uint64_t m_state;
    };

    void ValidateInlineMetadataAccessors()
    {
        // The argument of the layout is the log2 of the reference-count width: 2, 4 and 8 bits.
        for (uint8_t bits : {uint8_t{1}, uint8_t{2}, uint8_t{3}})
        {
            LxrSideMetadataLayout layout;
            SideMetadataManager metadata;
            // The data range does not need real memory: only the metadata planes are committed.
            const uintptr_t start = UINT64_C(0x20000000000);
            const size_t size = 2 * ImmixBlockGeometry::BlockBytes;
            SideMetadataResult created = LxrSideMetadataLayout::Create(bits, &layout);
            SideMetadataResult initialized = created == SideMetadataResult::Success
                ? metadata.Initialize(&layout) : created;
            SideMetadataResult committed = initialized == SideMetadataResult::Success
                ? metadata.CommitDataRange(start, size) : initialized;
            if (!Check(committed == SideMetadataResult::Success,
                    "side metadata initializes for the inline comparison"))
            {
                std::fprintf(stderr, "  bits %u: create %u, initialize %u, commit %u\n", static_cast<unsigned>(bits),
                    static_cast<unsigned>(created), static_cast<unsigned>(initialized), static_cast<unsigned>(committed));
                continue;
            }
            for (size_t kind = 0; kind < LxrSideMetadataLayout::SpecCount; kind++)
            {
                InlineMetadataComparison comparison(
                    metadata, layout, static_cast<LxrSideMetadataKind>(kind), start, size);
                comparison.Run();
            }
            metadata.Shutdown();
        }
    }

    // ---- P17 S3: the engine keeps no size table and no live bitmap in the product. These tests check the
    // replacements against the oracle that the test platform still keeps (GetObjectExtent, GetLiveBytes). ----

    class ArrayType final
    {
    public:
        ArrayType()
            : m_bytes(sizeof(MethodTable), 0), table(reinterpret_cast<MethodTable*>(m_bytes.data()))
        {
            table->m_baseSize = 24;
            table->m_flags = MTFlag_HasComponentSize | MTFlag_IsArray;
            table->m_componentSize = sizeof(void*);
        }

    private:
        std::vector<uint8_t> m_bytes;

    public:
        MethodTable* table;
    };

    uintptr_t StartOf(Object* object)
    {
        return reinterpret_cast<uintptr_t>(object) - sizeof(ObjHeader);
    }

    uint32_t ArrayLength(Object* object)
    {
        return *reinterpret_cast<uint32_t*>(
            reinterpret_cast<uint8_t*>(object) + ArrayBase::GetOffsetOfNumComponents());
    }

    Object* AllocateArray(LxrReferenceCounting& engine, LxrRcMutator* mutator, ArrayType& type, uint32_t length)
    {
        size_t size = type.table->GetBaseSize() + static_cast<size_t>(length) * type.table->RawGetComponentSize();
        Object* object = nullptr;
        if (!Check(engine.Allocate(mutator, size, &object) == LxrRcResult::Success, "array allocation succeeds"))
        {
            return nullptr;
        }
        object->RawSetMethodTable(type.table);
        *reinterpret_cast<uint32_t*>(
            reinterpret_cast<uint8_t*>(object) + ArrayBase::GetOffsetOfNumComponents()) = length;
        return object;
    }

    // A forwarded source loses its first field to the forwarding pointer, and an array keeps its length there.
    // The size of the source must still be right when the source is retired: the straddle lines of a mature
    // source are cleared with it, and a wrong size would leave the block unreleasable.
    void ValidateForwardedArraySizes()
    {
        ArrayType array;
        {
            LxrReferenceCounting engine;
            LxrRcMutator* mutator = nullptr;
            MovingRoots roots;
            int identity;
            if (!InitializeMoving(engine, &roots, 16, &mutator, &identity, 8))
            {
                return;
            }
            const uint32_t lengths[] = {40, 100, 33, 7, 509};
            size_t total = 0;
            for (uint32_t length : lengths)
            {
                Object* object = AllocateArray(engine, mutator, array, length);
                if (object == nullptr)
                {
                    return;
                }
                roots.objects.push_back(object);
                total += object->GetSize();
            }
            std::vector<Object*> sources = roots.objects;
            // An interior root into a source finds the object through its start bit and must take the size from
            // the copy, because the source lost its length.
            size_t interiorOffset = array.table->GetBaseSize() + 5 * sizeof(void*);
            roots.interior = reinterpret_cast<Object*>(reinterpret_cast<uint8_t*>(sources[2]) + interiorOffset);
            Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success, "nursery copy of arrays completes");
            Check(roots.interior == reinterpret_cast<Object*>(
                    reinterpret_cast<uint8_t*>(roots.objects[2]) + interiorOffset),
                "an interior root into a forwarded array source follows the copy");
            LxrNurseryMetrics nursery = engine.GetNurseryMetrics();
            Check(nursery.copiedObjects == 5 && nursery.copiedBytes == total && nursery.retiredSourceBytes == total,
                "retired source bytes equal the copied bytes although each source lost its array length");
            for (size_t index = 0; index < sources.size(); index++)
            {
                Object* destination = roots.objects[index];
                size_t expected = array.table->GetBaseSize() + lengths[index] * sizeof(void*);
                uintptr_t count;
                Check(destination != sources[index] && destination->GetSize() == expected &&
                        ArrayLength(destination) == lengths[index] &&
                        engine.LoadCount(destination, &count) == LxrRcResult::Success &&
                        count == (index == 2 ? 2 : 1),
                    "the copy keeps the exact array length and has one increment per root");
                Check(!engine.IsObjectStart(sources[index]) && engine.GetObjectExtent(sources[index]) == 0 &&
                        engine.IsObjectStart(destination) && engine.GetObjectExtent(destination) == expected,
                    "the source start is retired and the copy start is registered");
            }
        }
        {
            LxrReferenceCounting engine;
            LxrRcMutator* mutator = nullptr;
            MovingRoots roots;
            int identity;
            LxrRcConfiguration configuration{
                32 * ImmixBlockGeometry::BlockBytes, 4 * LxrLargeObjectSpace::MinimumObjectBytes,
                4, 4096, 4, 8,
            };
            configuration.evacuateMature = true;
            LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
            LxrRcObjectModel model{};
            model.context = &roots;
            model.finishRelocation = RelocateMoving;
            if (!Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success &&
                    engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                    "mature array engine initializes"))
            {
                return;
            }
            std::vector<Object*> sources;
            for (size_t index = 0; index < 64; index++)
            {
                Object* object = AllocateArray(engine, mutator, array, 509);
                if (object == nullptr)
                {
                    return;
                }
                if (index % 8 == 0)
                {
                    sources.push_back(object);
                }
            }
            roots.objects = sources;
            roots.interior = reinterpret_cast<Object*>(
                reinterpret_cast<uint8_t*>(sources[3]) + array.table->GetBaseSize() + 300 * sizeof(void*));
            if (!Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                    engine.Drain(Timeout) == LxrRcResult::Success &&
                    engine.CollectFull(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                    engine.Drain(Timeout) == LxrRcResult::Success, "mature array evacuation completes"))
            {
                return;
            }
            Check(roots.interior == reinterpret_cast<Object*>(
                    reinterpret_cast<uint8_t*>(roots.objects[3]) + array.table->GetBaseSize() + 300 * sizeof(void*)),
                "an interior root into a mature array source follows the copy");
            LxrMatureMetrics mature = engine.GetMatureMetrics();
            Check(mature.copiedObjects == 8 && mature.copiedBytes == 8 * 4096 &&
                    mature.retiredSourceBytes == mature.copiedBytes && mature.releasedBlocks == 8,
                "mature array sources retire with their exact size and their whole blocks are released");
            for (size_t index = 0; index < sources.size(); index++)
            {
                Check(roots.objects[index] != sources[index] && roots.objects[index]->GetSize() == 4096 &&
                        ArrayLength(roots.objects[index]) == 509,
                    "mature array destinations keep their length");
            }
        }
    }

    struct WalkCapture
    {
        LxrReferenceCounting* engine;
        MovingRoots* roots;
        std::vector<Object*> expected;
        bool checkStatistics;
        int rounds = 0;
    };

    struct WalkCollector
    {
        std::vector<Object*> objects;
        std::vector<size_t> sizes;
    };

    // Runs at the start of a pause, before the increments: an object has a count above zero exactly when an
    // earlier pause promoted it and it has not died. The walkers must visit exactly those objects.
    LxrRcResult CaptureWalk(LxrReferenceCounting* engine, void* context)
    {
        WalkCapture* walk = static_cast<WalkCapture*>(context);
        WalkCollector live;
        WalkCollector starts;
        auto collect = [](Object* object, size_t size, void* state)
        {
            WalkCollector* collector = static_cast<WalkCollector*>(state);
            collector->objects.push_back(object);
            collector->sizes.push_back(size);
            return true;
        };
        Check(engine->WalkObjects(collect, &live) == LxrRcResult::Success &&
                engine->WalkObjectStarts(collect, &starts) == LxrRcResult::Success, "ValidObject walkers run in a pause");
        std::vector<Object*> expected = walk->expected;
        std::sort(expected.begin(), expected.end());
        std::vector<Object*> visited = live.objects;
        std::sort(visited.begin(), visited.end());
        Check(visited == expected, "WalkObjects visits exactly the objects with a count above zero");
        bool sizes = true;
        for (size_t index = 0; index < live.objects.size(); index++)
        {
            sizes = sizes && live.sizes[index] == live.objects[index]->GetSize() &&
                live.sizes[index] == engine->GetObjectExtent(live.objects[index]);
        }
        Check(sizes, "WalkObjects passes the size that the oracle recorded");
        std::vector<Object*> raw = starts.objects;
        std::sort(raw.begin(), raw.end());
        Check(std::includes(raw.begin(), raw.end(), expected.begin(), expected.end()),
            "the raw walk visits every live object and may also visit stale bits");
        bool chain = true;
        for (size_t index = 1; index < expected.size(); index++)
        {
            chain = chain && engine->FindNextObject(expected[index - 1]) == expected[index];
        }
        Check(chain && (expected.empty() || engine->FindNextObject(expected.back()) == nullptr),
            "FindNextObject follows the counted objects in address order");
        if (walk->checkStatistics)
        {
            // The statistics come from block counters. A walk over the block states must give the same figures.
            size_t nonFree = 0;
            size_t freeLines = 0;
            size_t blocks = engine->GetImmixCapacityBytes() / ImmixBlockGeometry::BlockBytes;
            bool walked = true;
            for (size_t block = 0; block < blocks; block++)
            {
                ImmixBlockState state = ImmixBlockState::Unallocated;
                uint8_t unavailable = 0;
                bool nursery = false;
                walked = walked && engine->GetBlockStateForTest(block, &state, &unavailable, &nursery) ==
                    LxrRcResult::Success;
                nonFree += state != ImmixBlockState::Unallocated ? 1 : 0;
                freeLines += state == ImmixBlockState::Reusable ? ImmixBlockGeometry::LinesPerBlock - unavailable : 0;
            }
            size_t actualHeap = 0;
            size_t actualFragmentation = 0;
            Check(walked && engine->GetHeapStatistics(&actualHeap, &actualFragmentation) == LxrRcResult::Success &&
                    actualHeap == nonFree * ImmixBlockGeometry::BlockBytes &&
                    actualFragmentation == freeLines * ImmixBlockGeometry::LineBytes &&
                    engine->BlockCountersMatchWalk(),
                "heap statistics equal the figures that a walk over the block states gives");
        }
        walk->rounds++;
        return CaptureMoving(engine, walk->roots);
    }

    Object* TryAllocateQuietly(LxrReferenceCounting& engine, LxrRcMutator* mutator, Type& type)
    {
        Object* object = nullptr;
        if (engine.Allocate(mutator, type.table->GetBaseSize(), &object) != LxrRcResult::Success)
        {
            return nullptr;
        }
        object->RawSetMethodTable(type.table);
        return object;
    }

    // Random allocation, death and reuse of holes and blocks. After every pause the ValidObject plane, the counts and
    // the interior-pointer lookup are compared with the oracle. This is the proof for C3 (every reuse path clears the
    // ValidObject and the count bytes of the reused range) and for the stale-bit safety of FindContainingObject.
    void ValidateValidObjectBookkeeping(bool moving)
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        LxrRcConfiguration configuration{
            24 * ImmixBlockGeometry::BlockBytes, 4 * LxrLargeObjectSpace::MinimumObjectBytes,
            4, 4096, 4, 8,
        };
        configuration.evacuateNursery = moving;
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        LxrRcObjectModel model{};
        model.context = &roots;
        if (moving)
        {
            model.finishRelocation = RelocateMoving;
        }
        if (!Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success &&
                engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                "ValidObject bookkeeping engine initializes"))
        {
            return;
        }
        std::vector<Type*> types;
        for (size_t index = 0; index < 64; index++)
        {
            types.push_back(new Type(static_cast<uint32_t>(24 + 16 * index), 0));
        }
        uint64_t state = moving ? 0x2545F4914F6CDD1Dull : 0x9E3779B97F4A7C15ull;
        auto next = [&state]()
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            return state;
        };
        WalkCapture walk{&engine, &roots, {}, true};
        size_t slots = engine.GetReservationBytes() / sizeof(uintptr_t);
        uintptr_t arena = engine.GetReservationStart();
        size_t holeReuseChecks = 0;
        for (int round = 0; round < 60; round++)
        {
            size_t burst = 100 + next() % 120;
            for (size_t index = 0; index < burst; index++)
            {
                Object* object = TryAllocateQuietly(engine, mutator, *types[next() % types.size()]);
                if (object == nullptr)
                {
                    break;
                }
                if (next() % 100 < 18)
                {
                    roots.objects.push_back(object);
                }
            }
            // Before the pause: the range of every live or young object holds no foreign ValidObject bit
            // and no count except a straddle mark, whether the object sits in a fresh block or a reused hole.
            bool clean = true;
            for (Object* object : roots.objects)
            {
                size_t first = (StartOf(object) - arena) / sizeof(uintptr_t);
                size_t last = first + object->GetSize() / sizeof(uintptr_t);
                clean = clean && engine.FindNextValidSlot(first + 1) >= last;
                for (size_t index = first + 1; index < last; index++)
                {
                    uintptr_t address = arena + index * sizeof(uintptr_t);
                    uintptr_t count = 0;
                    clean = clean && engine.LoadCount(reinterpret_cast<Object*>(address + sizeof(ObjHeader)), &count) ==
                        LxrRcResult::Success && (count == 0 || (count == 1 && address % ImmixBlockGeometry::LineBytes == 0));
                }
                holeReuseChecks++;
            }
            Check(clean, "no ValidObject bit and no stray count lies inside a live or young object");
            if (next() % 8 == 0 && !roots.objects.empty())
            {
                roots.objects.erase(roots.objects.begin() + static_cast<std::ptrdiff_t>(next() % roots.objects.size()));
            }
            for (size_t index = 0; index < roots.objects.size();)
            {
                if (next() % 100 < 22)
                {
                    roots.objects.erase(roots.objects.begin() + static_cast<std::ptrdiff_t>(index));
                }
                else
                {
                    index++;
                }
            }
            while (roots.objects.size() > 300)
            {
                roots.objects.erase(roots.objects.begin());
            }
            bool full = round % 9 == 8;
            walk.checkStatistics = !full;
            LxrRcResult result = full ? engine.CollectFull(CaptureWalk, &walk, Timeout)
                : engine.Collect(CaptureWalk, &walk, Timeout);
            if (!Check(result == LxrRcResult::Success && engine.Drain(Timeout) == LxrRcResult::Success,
                    "random pause completes"))
            {
                break;
            }
            walk.expected = roots.objects;
            std::vector<Object*> sorted = roots.objects;
            std::sort(sorted.begin(), sorted.end());
            size_t counted = 0;
            bool consistent = true;
            for (size_t index = engine.FindNextValidSlot(0); index < slots; index = engine.FindNextValidSlot(index + 1))
            {
                Object* object = reinterpret_cast<Object*>(arena + index * sizeof(uintptr_t) + sizeof(ObjHeader));
                uintptr_t count = 0;
                consistent = consistent && engine.LoadCount(object, &count) == LxrRcResult::Success;
                if (count != 0)
                {
                    counted++;
                    consistent = consistent && std::binary_search(sorted.begin(), sorted.end(), object) &&
                        engine.GetObjectExtent(object) == object->GetSize();
                }
                else
                {
                    // A bit without a count is a stale bit of a dead young object: the oracle has reclaimed it.
                    consistent = consistent && engine.GetObjectExtent(object) == 0;
                }
            }
            Check(consistent && counted == roots.objects.size(),
                "after a pause every counted ValidObject bit is a live object and every other bit is stale");
            bool lookups = true;
            for (Object* object : roots.objects)
            {
                size_t size = object->GetSize();
                for (size_t probe = 0; probe < 6; probe++)
                {
                    uintptr_t offset = sizeof(ObjHeader) + next() % (size - sizeof(ObjHeader));
                    Object* found = nullptr;
                    lookups = lookups &&
                        engine.FindContainingObject(StartOf(object) + offset, &found) == LxrRcResult::Success &&
                        found == object;
                }
            }
            Check(lookups, "random interior addresses of live objects resolve to their object next to stale bits");
        }
        Check(holeReuseChecks > 1000 && walk.rounds == 60, "the random run exercised reuse and every pause walk");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "bookkeeping owner detaches");
        for (Type* type : types)
        {
            delete type;
        }
    }

    // A dead young object keeps its ValidObject bit. The bit must not count as live for any query.
    void ValidateStaleBitQueries()
    {
        Type leaf(24, 0);
        Type link(24, 1);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Object* kept = Allocate(engine, mutator, link);
        Object* dead = Allocate(engine, mutator, leaf);
        if (kept == nullptr || dead == nullptr || !Epoch(engine, {kept}))
        {
            return;
        }
        Object* containing = nullptr;
        Check(!engine.IsObjectStart(dead) && engine.GetObjectExtent(dead) == 0 && Count(engine, dead) == 0,
            "a dead young object never had a ValidObject bit, and the oracle has reclaimed it");
        Check(!engine.IsMarked(dead) && engine.GetLiveObjectSize(dead) == 0 && engine.GetLiveObjectSize(kept) == 24,
            "a stale bit is neither marked nor live, and the live size comes from the object");
        Check(engine.FindContainingObject(StartOf(kept) + 16, &containing) == LxrRcResult::Success && containing == kept,
            "interior lookup of a live neighbour ignores the stale bit");
        Check(engine.GetUsedBytesEstimate() >= ImmixBlockGeometry::BlockBytes,
            "the block-count estimate counts the block that holds the objects");
        // A remembered slot of a dead holder can lie in memory that a later allocation has overwritten, while the
        // stale bit of the old object is still the nearest bit. The lookup must not read that memory.
        memset(dead, 0, 24);
        Object* stale = reinterpret_cast<Object*>(0x1);
        Check(engine.FindContainingObject(StartOf(dead) + 16, &stale, true) == LxrRcResult::InvalidArgument &&
                stale == nullptr &&
                engine.FindContainingObject(StartOf(dead) + 64, &stale, true) == LxrRcResult::InvalidArgument &&
                engine.FindContainingObject(StartOf(kept) + 16, &containing, true) == LxrRcResult::Success &&
                containing == kept,
            "a counted lookup rejects a stale bit without reading the overwritten memory");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "stale-bit owner detaches");
    }

    struct SweepGate
    {
        LxrReferenceCounting* engine = nullptr;
        MethodTable* collectible = nullptr;
        Object* loader = nullptr;
        std::thread finalizer;
        std::atomic<bool> poisoned{false};
        std::atomic<uint64_t> cycleObjectsAtPoison{UINT64_MAX};
    };

    SweepGate* g_sweepGate = nullptr;

    Object** GateLoaderSlot(MethodTable*)
    {
        return &g_sweepGate->loader;
    }

    // The runtime frees the type data of a collectible type from a finalizer thread, which the collector signals
    // when the pause resumes. The thread here does what that thread does: it waits for the collector to allow
    // finalization (IsCycleSweepPending is false) and then destroys the type data.
    void GateResume(bool, void* context)
    {
        SweepGate* gate = static_cast<SweepGate*>(context);
        if (gate->engine == nullptr || gate->engine->GetTraceMetrics().finalMarks == 0 || gate->finalizer.joinable())
        {
            return;
        }
        gate->finalizer = std::thread([gate]()
        {
            while (gate->engine->IsCycleSweepPending())
            {
                std::this_thread::yield();
            }
            gate->cycleObjectsAtPoison.store(gate->engine->GetTraceMetrics().cycleObjects);
            gate->collectible->m_baseSize = 0;
            gate->poisoned.store(true);
        });
    }

    void ValidateCycleSweepGate()
    {
        Type link(24, 1);
        Type plain(24, 1);
        Type leaf(24, 0);
        link.table->m_flags |= MTFlag_Collectible;
        SweepGate gate;
        g_sweepGate = &gate;
        LxrReferenceCounting engine;
        gate.engine = &engine;
        gate.collectible = link.table;
        LxrRcConfiguration configuration{
            16 * ImmixBlockGeometry::BlockBytes, 8 * LxrLargeObjectSpace::MinimumObjectBytes, 16, 1024, 4, 8,
        };
        LxrPauseRuntimeAdapter adapter{Suspend, GateResume, &gate, false};
        LxrRcObjectModel model{};
        model.getLoaderAllocatorSlot = GateLoaderSlot;
        model.context = &gate;
        int identity;
        LxrRcMutator* mutator = nullptr;
        if (!Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success &&
                engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success, "sweep gate engine initializes"))
        {
            return;
        }
        gate.loader = Allocate(engine, mutator, leaf);
        Object* left = Allocate(engine, mutator, link);
        Object* right = Allocate(engine, mutator, link);
        Object* live = Allocate(engine, mutator, plain);
        Object* other = Allocate(engine, mutator, leaf);
        if (gate.loader == nullptr || left == nullptr || right == nullptr || live == nullptr || other == nullptr)
        {
            return;
        }
        Check(Store(engine, mutator, Field(left), right) && Store(engine, mutator, Field(right), left) &&
                Store(engine, mutator, Field(live), other), "dead cycle of collectible objects initializes");
        std::vector<Object*> roots{left, live};
        std::vector<Object*> remaining{live};
        Roots rootsB{&remaining};
        if (!Epoch(engine, roots) ||
            !Check(engine.HoldTrace(true) == LxrRcResult::Success &&
                engine.BeginTrace(Capture, &rootsB, Timeout) == LxrRcResult::Success, "the trace starts"))
        {
            return;
        }
        // The overwritten field gives the next pause a logged decrement. Holding the log reader keeps the lazy
        // decrements, and so the cycle sweep that follows them, from running after the final pause.
        Check(Store(engine, mutator, Field(live), nullptr), "a logged decrement exists");
        engine.HoldLogReaderForTest(true);
        Check(engine.HoldTrace(false) == LxrRcResult::Success &&
                engine.FinishTrace(Capture, &rootsB, Timeout) == LxrRcResult::Success,
            "the final pause completes while the lazy decrements wait");
        for (int attempt = 0; attempt < 5000 && !engine.IsLogReaderHeldForTest(); attempt++)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(1));
        }
        Check(engine.IsLogReaderHeldForTest() && engine.IsCycleSweepPending(),
            "the cycle sweep is pending after the tracing pause");
        std::this_thread::sleep_for(std::chrono::milliseconds(100));
        Check(!gate.poisoned.load(), "the runtime cannot free type data while the cycle sweep is pending");
        engine.HoldLogReaderForTest(false);
        Check(engine.Drain(Timeout) == LxrRcResult::Success, "the lazy tail and the cycle sweep finish");
        if (gate.finalizer.joinable())
        {
            gate.finalizer.join();
        }
        Check(gate.poisoned.load() && gate.cycleObjectsAtPoison.load() == 3 &&
                engine.GetTraceMetrics().cycleObjects == 3 && engine.GetTraceMetrics().cycleBytes == 72,
            "type data was freed only after the sweep had read the sizes of all three dead cycle objects");
        Check(!engine.IsCycleSweepPending() && Count(engine, left) == 0 && Count(engine, right) == 0 &&
                Count(engine, gate.loader) == 0, "the dead cycle and its loader allocator object are reclaimed");
        link.table->m_baseSize = 24;
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "sweep gate owner detaches");
    }

    void ValidateS3Bookkeeping()
    {
        ValidateForwardedArraySizes();
        ValidateStaleBitQueries();
        ValidateValidObjectBookkeeping(false);
        ValidateValidObjectBookkeeping(true);
        ValidateCycleSweepGate();
    }

    ImmixBlockState BlockStateOf(LxrReferenceCounting& engine, size_t block, uint8_t* unavailable = nullptr)
    {
        ImmixBlockState state = ImmixBlockState::Unallocated;
        uint8_t lines = 0;
        bool nursery = false;
        Check(engine.GetBlockStateForTest(block, &state, &lines, &nursery) == LxrRcResult::Success,
            "a block state is readable");
        if (unavailable != nullptr)
        {
            *unavailable = lines;
        }
        return state;
    }

    size_t BlockOfObject(LxrReferenceCounting& engine, Object* object)
    {
        return (StartOf(object) - engine.GetReservationStart()) / ImmixBlockGeometry::BlockBytes;
    }

    bool WaitForSweepHold(LxrReferenceCounting& engine)
    {
        for (int attempt = 0; attempt < 5000 && !engine.IsSweepHeldForTest(); attempt++)
        {
            std::this_thread::sleep_for(std::chrono::milliseconds(1));
        }
        return Check(engine.IsSweepHeldForTest(), "the lazy sweep reaches its first flagged block");
    }

    // Three nursery blocks of small objects. The first object (block 0) and the last one (block 2) are roots, so the
    // pause promotes those two blocks in place and no object in the middle block survives.
    struct ThreeBlocks
    {
        std::vector<Object*> objects;
        std::vector<Object*> roots;
        size_t first = 0;
        size_t middle = 0;
        size_t last = 0;
    };

    bool FillThreeBlocks(LxrReferenceCounting& engine, LxrRcMutator* mutator, Type& leaf, ThreeBlocks* blocks)
    {
        for (size_t index = 0; index < 3500; index++)
        {
            Object* object = Allocate(engine, mutator, leaf);
            if (object == nullptr)
            {
                return false;
            }
            blocks->objects.push_back(object);
        }
        blocks->roots = {blocks->objects.front(), blocks->objects.back()};
        blocks->first = BlockOfObject(engine, blocks->objects.front());
        blocks->middle = BlockOfObject(engine, blocks->objects[1750]);
        blocks->last = BlockOfObject(engine, blocks->objects.back());
        return Check(blocks->first != blocks->middle && blocks->middle != blocks->last &&
                blocks->first != blocks->last && engine.GetCleanNurseryBlocks() == 3,
            "the nursery fills three different blocks");
    }

    // The pause looks at the allocated blocks only. A nursery block without a promoted object goes back to the free
    // pool in the pause. A block with a promoted object keeps its state after the pause.
    //
    // P28 (F9). A block that is not free is a candidate for allocation from the start of the epoch, as in the Java LXR: the
    // allocator reads the counts of its lines when it takes it, and hands out only lines without a count. So the first
    // scenario lets the mutator allocate before the lazy sweep has looked at the promoted blocks: the promoted objects stay
    // intact, the blocks are used, and the sweep leaves a block that an allocator took in this epoch to the next pause. In the
    // second scenario the sweep finishes first: it makes the blocks Reusable with their free-line count, and they are used.
    void ValidatePauseReleaseAndLazyReuse(bool allocateWhileHeld)
    {
        Type leaf(24, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        ThreeBlocks blocks;
        if (!FillThreeBlocks(engine, mutator, leaf, &blocks))
        {
            return;
        }
        engine.HoldSweepForTest(true);
        Roots roots{&blocks.roots};
        if (!Check(engine.Collect(Capture, &roots, Timeout) == LxrRcResult::Success,
                "the pause completes while the lazy sweep is held"))
        {
            engine.HoldSweepForTest(false);
            return;
        }
        bool held = WaitForSweepHold(engine);
        Check(engine.GetFreshBlocksReleasedInPauseForTest() == 1 && engine.GetMetrics().blocksReleased == 1,
            "the pause released the nursery block that no object promoted, and only that block");
        uint8_t lines = 0;
        Check(BlockStateOf(engine, blocks.middle) == ImmixBlockState::Unallocated && engine.GetNonFreeBlocks() == 2,
            "the released block is free and the counter holds the two promoted blocks");
        Check(BlockStateOf(engine, blocks.first) == ImmixBlockState::Unmarked &&
                BlockStateOf(engine, blocks.last) == ImmixBlockState::Unmarked,
            "a block with a promoted object keeps its state; the pause does not count its lines");
        if (allocateWhileHeld)
        {
            bool unprocessedUsed = false;
            bool overlap = false;
            for (size_t index = 0; index < 2800; index++)
            {
                Object* object = Allocate(engine, mutator, leaf);
                if (object == nullptr)
                {
                    break;
                }
                size_t block = BlockOfObject(engine, object);
                unprocessedUsed = unprocessedUsed || block == blocks.first || block == blocks.last;
                for (Object* root : blocks.roots)
                {
                    overlap = overlap || object == root;
                }
            }
            uintptr_t count = 0;
            bool intact = true;
            for (Object* root : blocks.roots)
            {
                intact = intact && engine.IsObjectStart(root) &&
                    engine.LoadCount(root, &count) == LxrRcResult::Success && count == 1;
            }
            Check(unprocessedUsed && !overlap && intact,
                "a promoted block is handed out before the lazy sweep has looked at it, and its promoted objects stay intact");
            engine.HoldSweepForTest(false);
            Check(held && engine.Drain(Timeout) == LxrRcResult::Success, "the lazy sweep finishes");
            Check(BlockStateOf(engine, blocks.first) != ImmixBlockState::Unallocated &&
                    BlockStateOf(engine, blocks.last) != ImmixBlockState::Unallocated &&
                    engine.BlockCountersMatchWalk(),
                "the sweep leaves a block that an allocator took in this epoch, and the counters equal a walk");
        }
        else
        {
            engine.HoldSweepForTest(false);
            Check(held && engine.Drain(Timeout) == LxrRcResult::Success, "the lazy sweep finishes");
            Check(BlockStateOf(engine, blocks.first, &lines) == ImmixBlockState::Reusable && lines >= 1 && lines <= 4 &&
                    BlockStateOf(engine, blocks.last, &lines) == ImmixBlockState::Reusable && lines >= 1 && lines <= 4,
                "the lazy sweep makes a block with a promoted object Reusable with its free-line count");
            size_t nonFree;
            size_t freeLines;
            Check(engine.BlockCountersMatchWalk(&nonFree, &freeLines) && nonFree == 2 && freeLines >= 2 * 124,
                "the block counters equal a walk after the lazy sweep");
            bool reused = false;
            for (size_t index = 0; index < 3000; index++)
            {
                Object* object = Allocate(engine, mutator, leaf);
                if (object == nullptr)
                {
                    break;
                }
                size_t block = BlockOfObject(engine, object);
                reused = reused || block == blocks.first || block == blocks.last;
            }
            Check(reused, "a block that the lazy sweep made Reusable is used for allocation in the same epoch");
        }
        Check(Epoch(engine, blocks.roots) && engine.BlockCountersMatchWalk(),
            "the next pause keeps the counters and the promoted blocks");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "S4 owner detaches");
    }

    struct PauseObservation
    {
        const std::vector<Object*>* roots;
        size_t block;
        bool swept;
        bool held;
        bool counters;
    };

    LxrRcResult CaptureObserving(LxrReferenceCounting* engine, void* context)
    {
        PauseObservation* observation = static_cast<PauseObservation*>(context);
        observation->held = engine->IsSweepHeldForTest();
        observation->swept = BlockStateOf(*engine, observation->block) == ImmixBlockState::Reusable;
        observation->counters = engine->BlockCountersMatchWalk();
        Roots roots{observation->roots};
        return Capture(engine, &roots);
    }

    // A pause that is requested while the lazy sweep runs must wait for it. The sweep changes block states, and the
    // pause flips the epoch, so the two must not overlap (the flip assertion of the Debug build checks this too).
    void ValidatePauseWaitsForLazySweep()
    {
        Type leaf(24, 0);
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        ThreeBlocks blocks;
        if (!FillThreeBlocks(engine, mutator, leaf, &blocks))
        {
            return;
        }
        engine.HoldSweepForTest(true);
        Roots roots{&blocks.roots};
        if (!Check(engine.Collect(Capture, &roots, Timeout) == LxrRcResult::Success, "the first pause completes") ||
            !WaitForSweepHold(engine))
        {
            engine.HoldSweepForTest(false);
            return;
        }
        uint64_t before = engine.GetMetrics().epochs;
        PauseObservation observation{&blocks.roots, blocks.first, false, true, false};
        std::atomic<bool> done{false};
        LxrRcResult second = LxrRcResult::InvalidPhase;
        std::thread requester([&]()
        {
            second = engine.Collect(CaptureObserving, &observation, Timeout);
            done.store(true);
        });
        std::this_thread::sleep_for(std::chrono::milliseconds(300));
        Check(!done.load() && engine.GetPhase() == LxrRcPhase::Mutator && engine.GetMetrics().epochs == before,
            "a pause that is requested while the lazy sweep runs does not start");
        engine.HoldSweepForTest(false);
        requester.join();
        Check(second == LxrRcResult::Success && engine.GetMetrics().epochs == before + 1,
            "the pause runs after the lazy sweep has finished");
        Check(!observation.held && observation.swept && observation.counters,
            "when the pause started, the sweep had finished, the promoted block was Reusable and the counters held");
        Check(engine.Drain(Timeout) == LxrRcResult::Success && engine.BlockCountersMatchWalk(),
            "the counters hold after the second pause and its lazy sweep");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "S4 waiting owner detaches");
    }

    struct CounterCapture
    {
        LxrReferenceCounting* engine;
        MovingRoots* roots;
        int rounds = 0;
        int mismatches = 0;
        int pageMismatches = 0;
    };

    LxrRcResult CaptureCounters(LxrReferenceCounting* engine, void* context)
    {
        CounterCapture* capture = static_cast<CounterCapture*>(context);
        capture->rounds++;
        capture->mismatches += engine->BlockCountersMatchWalk() ? 0 : 1;
        return CaptureMoving(engine, capture->roots);
    }

    // The block counters (blocks in use, free lines of Reusable blocks) must equal a walk over all blocks after any
    // sequence of allocation, death, promotion, copy and release. Kind 0 does not move objects, kind 1 copies the
    // nursery, kind 2 also evacuates mature blocks.
    void ValidateBlockCounters(int kind)
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        LxrRcConfiguration configuration{
            24 * ImmixBlockGeometry::BlockBytes, 4 * LxrLargeObjectSpace::MinimumObjectBytes,
            4, 4096, 4, 8,
        };
        configuration.evacuateNursery = kind == 1;
        configuration.evacuateMature = kind == 2;
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        LxrRcObjectModel model{};
        model.context = &roots;
        if (kind != 0)
        {
            model.finishRelocation = RelocateMoving;
        }
        if (!Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success &&
                engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                "block counter engine initializes"))
        {
            return;
        }
        std::vector<Type*> types;
        for (size_t index = 0; index < 48; index++)
        {
            types.push_back(new Type(static_cast<uint32_t>(24 + 24 * index), 0));
        }
        Type large(static_cast<uint32_t>(LxrLargeObjectSpace::MinimumObjectBytes + 64), 0);
        uint64_t state = 0xD1B54A32D192ED03ull + static_cast<uint64_t>(kind);
        auto next = [&state]()
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            return state;
        };
        CounterCapture capture{&engine, &roots};
        size_t pausesChecked = 0;
        for (int round = 0; round < 45; round++)
        {
            size_t burst = 60 + next() % 200;
            for (size_t index = 0; index < burst; index++)
            {
                Object* object = TryAllocateQuietly(engine, mutator, *types[next() % types.size()]);
                if (object == nullptr)
                {
                    break;
                }
                if (next() % 100 < 15)
                {
                    roots.objects.push_back(object);
                }
            }
            if (next() % 6 == 0)
            {
                Object* object = TryAllocateQuietly(engine, mutator, large);
                if (object != nullptr && next() % 2 == 0)
                {
                    roots.objects.push_back(object);
                }
            }
            for (size_t index = 0; index < roots.objects.size();)
            {
                if (next() % 100 < 25)
                {
                    roots.objects.erase(roots.objects.begin() + static_cast<std::ptrdiff_t>(index));
                }
                else
                {
                    index++;
                }
            }
            while (roots.objects.size() > 250)
            {
                roots.objects.erase(roots.objects.begin());
            }
            bool full = kind == 2 ? round % 3 == 2 : round % 9 == 8;
            LxrRcResult result = full ? engine.CollectFull(CaptureCounters, &capture, Timeout)
                : engine.Collect(CaptureCounters, &capture, Timeout);
            if (!Check(result == LxrRcResult::Success && engine.Drain(Timeout) == LxrRcResult::Success,
                    "random pause of the block counter run completes"))
            {
                break;
            }
            size_t pages = 0;
            size_t heap = 0;
            size_t fragmentation = 0;
            size_t nonFree = 0;
            Check(engine.BlockCountersMatchWalk(&nonFree, nullptr) && engine.GetMaturePages(&pages) ==
                    LxrRcResult::Success && engine.GetHeapStatistics(&heap, &fragmentation) == LxrRcResult::Success &&
                    pages * LxrLargeObjectSpace::PageBytes == heap &&
                    heap >= nonFree * ImmixBlockGeometry::BlockBytes,
                "after the lazy sweep the counters equal a walk, and the page count equals the heap size");
            pausesChecked++;
        }
        Check(capture.rounds == static_cast<int>(pausesChecked) && capture.mismatches == 0,
            "at the start of every pause the block counters equal a walk over all blocks");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "block counter owner detaches");
        for (Type* type : types)
        {
            delete type;
        }
    }

    void ValidateS4Sweep()
    {
        ValidatePauseReleaseAndLazyReuse(false);
        ValidatePauseReleaseAndLazyReuse(true);
        ValidatePauseWaitsForLazySweep();
        for (int kind = 0; kind < 3; kind++)
        {
            ValidateBlockCounters(kind);
        }
    }

    // ---- P17 S5b: the pause takes to-space beyond the admitted capacity and gives the free tail back ----

    constexpr size_t OvershootBlock = ImmixBlockGeometry::BlockBytes;
    std::atomic<bool> OvershootRefuse{false};

    bool OvershootCanAdmit(size_t, size_t, size_t, void*)
    {
        return !OvershootRefuse.load(std::memory_order_acquire);
    }

    bool InitializeOvershoot(
        LxrReferenceCounting& engine, MovingRoots* roots, size_t initialBlocks, size_t reserveBlocks,
        LxrRcMutator** mutator, void* identity, size_t hardLimitBytes = 0, bool overshoot = true,
        bool mature = false)
    {
        LxrRcConfiguration configuration{
            initialBlocks * OvershootBlock, 2 * LxrLargeObjectSpace::MinimumObjectBytes, 4, 4096, 4, 8,
            reserveBlocks * OvershootBlock, 2 * LxrLargeObjectSpace::MinimumObjectBytes, hardLimitBytes,
        };
        configuration.evacuateNursery = true;
        configuration.evacuateMature = mature;
        configuration.lineRecycling = false;
        configuration.overshootToSpace = overshoot;
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        LxrRcObjectModel model{};
        model.context = roots;
        model.finishRelocation = RelocateMoving;
        model.canAdmitRange = OvershootCanAdmit;
        return Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success,
                "overshoot engine initializes") &&
            Check(engine.AttachMutator(identity, mutator) == LxrRcResult::Success, "overshoot mutator attaches");
    }

    uint64_t* PayloadOf(Object* object)
    {
        return reinterpret_cast<uint64_t*>(reinterpret_cast<uint8_t*>(object) + sizeof(void*));
    }

    uintptr_t PlaneAddress(const LxrSideMetadataLayout& layout, LxrSideMetadataKind kind, uintptr_t dataAddress)
    {
        const SideMetadataSpec* spec = layout.GetSpec(kind);
        uintptr_t entry = dataAddress >> spec->log_bytes_per_value;
        uintptr_t offset = spec->log_bits_per_value <= 3 ? entry >> (3 - spec->log_bits_per_value)
            : entry << (spec->log_bits_per_value - 3);
        return spec->base_address + offset;
    }

    // 1: a page of the range is resident, 0: no page is, -1: the platform cannot tell.
    int ResidentPages(uintptr_t address, size_t bytes)
    {
#if defined(__linux__)
        uintptr_t page = static_cast<uintptr_t>(sysconf(_SC_PAGESIZE));
        uintptr_t first = address & ~(page - 1);
        size_t length = ((address + bytes + page - 1) & ~(page - 1)) - first;
        std::vector<unsigned char> pages(length / page);
        if (mincore(reinterpret_cast<void*>(first), length, pages.data()) != 0)
        {
            return -1;
        }
        for (unsigned char value : pages)
        {
            if ((value & 1) != 0)
            {
                return 1;
            }
        }
        return 0;
#else
        (void)address;
        (void)bytes;
        return -1;
#endif
    }

    struct PauseWatch
    {
        MovingRoots* roots;
        size_t capacity = 0;
        size_t borrowed = 0;
        bool counters = false;
        // Dirty the planes and the data of this block range before the copy (a pause that borrows blocks).
        size_t dirtyFirst = 0;
        size_t dirtyEnd = 0;
        uint8_t pattern = 0;
        bool verifyZero = false;
        size_t nonZeroBytes = 0;
    };

    LxrRcResult CaptureWatching(LxrReferenceCounting* engine, void* context)
    {
        PauseWatch* watch = static_cast<PauseWatch*>(context);
        watch->capacity = engine->GetImmixCapacityBytes();
        watch->borrowed = engine->GetBorrowedBytes();
        watch->counters = engine->BlockCountersMatchWalk();
        if (watch->dirtyEnd > watch->dirtyFirst && watch->capacity >= watch->dirtyEnd * OvershootBlock)
        {
            LxrSideMetadataLayout layout;
            LxrSideMetadataLayout::Create(3, &layout);
            for (size_t block = watch->dirtyFirst; block < watch->dirtyEnd; block++)
            {
                uintptr_t address = engine->GetReservationStart() + block * OvershootBlock;
                for (LxrSideMetadataKind kind : {LxrSideMetadataKind::ReferenceCount, LxrSideMetadataKind::ValidObject})
                {
                    uintptr_t plane = PlaneAddress(layout, kind, address);
                    size_t bytes = kind == LxrSideMetadataKind::ReferenceCount ? OvershootBlock / 8 : OvershootBlock / 64;
                    for (size_t offset = 0; offset < bytes; offset += 64)
                    {
                        volatile uint8_t* cell = reinterpret_cast<volatile uint8_t*>(plane + offset);
                        if (watch->verifyZero)
                        {
                            watch->nonZeroBytes += *cell != 0 ? 1 : 0;
                        }
                        else
                        {
                            *cell = watch->pattern;
                        }
                    }
                }
                if (!watch->verifyZero)
                {
                    for (size_t offset = 0; offset < OvershootBlock; offset += 4096)
                    {
                        *reinterpret_cast<volatile uint8_t*>(address + offset) = 0;
                    }
                }
            }
        }
        return CaptureMoving(engine, watch->roots);
    }

    bool RunPauseAndDrain(
        LxrReferenceCounting& engine, LxrRcCaptureRoots capture, void* context, const char* message, bool full = false)
    {
        LxrRcResult collected = full ? engine.CollectFull(capture, context, Timeout)
            : engine.Collect(capture, context, Timeout);
        LxrRcResult drained = collected == LxrRcResult::Success ? engine.Drain(Timeout) : collected;
        if (drained != LxrRcResult::Success)
        {
            std::fprintf(stderr, "pause result=%u failure=%u\n", static_cast<unsigned>(drained),
                static_cast<unsigned>(engine.GetFailure()));
        }
        return Check(drained == LxrRcResult::Success, message);
    }

    void ValidateOvershootPolicy()
    {
        LxrCollectionPolicy policy;
        LxrPolicyPressure pressure{};
        pressure.contiguous = true;
        pressure.admittedPages = 100000;
        pressure.usedPages = 1000;
        pressure.immixFreePages = 500;
        pressure.copyReservePages = 1000;
        Check(policy.Trigger(pressure) == LxrCollectionCause::HeapFull,
            "without overshoot a to-space reserve of 1000 pages against 500 free Immix pages triggers");
        pressure.overshoot = true;
        Check(policy.Trigger(pressure) == LxrCollectionCause::None,
            "with overshoot no reserve is kept: 500 free Immix pages do not trigger");
        pressure.immixFreePages = LxrCollectionPolicy::OvershootFreePages + 1;
        Check(policy.Trigger(pressure) == LxrCollectionCause::None, "with overshoot more than one free block does not trigger");
        pressure.immixFreePages = LxrCollectionPolicy::OvershootFreePages;
        Check(policy.Trigger(pressure) == LxrCollectionCause::HeapFull,
            "with overshoot the heap is full when no more than one block is free");
        pressure.immixFreePages = 500;
        pressure.heapFull = true;
        Check(policy.Trigger(pressure) == LxrCollectionCause::HeapFull, "a failed allocation still triggers with overshoot");
        pressure.heapFull = false;
        pressure.usedPages = pressure.admittedPages;
        Check(policy.Trigger(pressure) == LxrCollectionCause::HeapFull, "used pages at the admitted pages still trigger with overshoot");
    }

    // One pause that has to copy two 24 KiB objects into a heap with no free block: the pause admits blocks, copies
    // into them, gives the unused tail back, and a later pause returns the rest when the survivors are dead.
    void ValidateOvershootBorrowAndGiveBack()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        if (!InitializeOvershoot(engine, &roots, 2, 24, &mutator, &identity))
        {
            return;
        }
        Check(engine.IsOvershootEnabled() && engine.GetImmixFloorBytes() == 2 * OvershootBlock,
            "no hard limit and a reservation above the initial capacity enable overshoot");
        Type big(24576, 0);
        Object* first = Allocate(engine, mutator, big);
        Object* second = Allocate(engine, mutator, big);
        if (first == nullptr || second == nullptr)
        {
            return;
        }
        *PayloadOf(first) = 0x1111;
        *PayloadOf(second) = 0x2222;
        roots.objects = {first, second};
        Check(engine.GetImmixFreePages() == 0, "the nursery fills the admitted capacity");
        uintptr_t arena = engine.GetReservationStart();
        PauseWatch watch{&roots};
        engine.RequestBorrow(64);
        RunPauseAndDrain(engine, CaptureWatching, &watch, "the pause that borrows completes");
        Check(watch.capacity == 10 * OvershootBlock && watch.borrowed == 8 * OvershootBlock && watch.counters,
            "the pause has admitted eight blocks beyond the capacity when it captures the roots");
        LxrNurseryMetrics nursery = engine.GetNurseryMetrics();
        if (!Check(nursery.copiedBytes == 2 * 24576 && nursery.copyFailures == 0 &&
                roots.objects[0] != first && roots.objects[1] != second,
                "both objects are copied into the borrowed blocks"))
        {
            return;
        }
        size_t highest = (std::max)(BlockOfObject(engine, roots.objects[0]), BlockOfObject(engine, roots.objects[1]));
        size_t capacity = engine.GetImmixCapacityBytes();
        // A block that holds a survivor, and every block below it, stays. Only the free tail above goes back.
        if (!Check(highest >= 2 && capacity == (highest + 1) * OvershootBlock &&
                engine.GetBorrowedBytes() == capacity - 2 * OvershootBlock,
                "the capacity ends after the highest block that holds a survivor"))
        {
            return;
        }
        Check(*PayloadOf(roots.objects[0]) == 0x1111 && *PayloadOf(roots.objects[1]) == 0x2222,
            "the survivors keep their payload");
        Check(engine.OwnsAddress(arena + capacity - 1) && engine.IsDataOwnedForTest(arena + capacity - 1) &&
                !engine.OwnsAddress(arena + capacity) && !engine.IsDataOwnedForTest(arena + capacity) &&
                !engine.IsDataOwnedForTest(arena + 9 * OvershootBlock),
            "the returned blocks are no Immix space and no owned metadata range any more");
        Check(engine.ProcessQueuedTraceReferenceForTest(arena + capacity + 64) == LxrRcResult::Success &&
                engine.GetFailure() == LxrRcResult::Success,
            "queued trace work that names an object of a returned block finds a dead object and does not fail");
        Check(engine.GetOvershootMetric(LxrReferenceCounting::BorrowPauses) == 1 &&
                engine.GetOvershootMetric(LxrReferenceCounting::BorrowBytesLast) == 8 * OvershootBlock &&
                engine.GetOvershootMetric(LxrReferenceCounting::GiveBackSuccesses) == 1 &&
                engine.GetOvershootMetric(LxrReferenceCounting::GiveBackBytesLast) == 10 * OvershootBlock - capacity &&
                engine.GetOvershootMetric(LxrReferenceCounting::GiveBackComplete) == 0 &&
                engine.GetOvershootMetric(LxrReferenceCounting::BorrowedNow) == capacity - 2 * OvershootBlock,
            "the borrow and the partial give-back are counted");
        Check(engine.BlockCountersMatchWalk(), "the block counters equal a walk after the give-back");
        // No allocator can take a block of the returned range.
        Type tiny(24, 0);
        size_t allocated = 0;
        bool outside = false;
        for (size_t index = 0; index < 20000; index++)
        {
            Object* object = TryAllocateQuietly(engine, mutator, tiny);
            if (object == nullptr)
            {
                break;
            }
            allocated++;
            outside = outside || StartOf(object) >= arena + engine.GetImmixCapacityBytes();
        }
        Check(allocated > 1000 && !outside, "allocation fills the capacity and never takes a block of the returned range");
        // The survivors die. The next pause cannot return their blocks yet (the counts fall after it).
        roots.objects.clear();
        RunPauseAndDrain(engine, CaptureMoving, &roots, "the pause while the survivors are still counted completes");
        Check(engine.GetImmixCapacityBytes() == capacity,
            "while the survivors are counted, their blocks are in use and stay");
        // Their blocks are free now, but above the kept capacity: mutators do not take them, and the next pause that has
        // to copy uses them before it borrows anything.
        Object* third = Allocate(engine, mutator, big);
        Object* fourth = Allocate(engine, mutator, big);
        if (third == nullptr || fourth == nullptr)
        {
            return;
        }
        if (!Check(BlockOfObject(engine, third) < 2 && BlockOfObject(engine, fourth) < 2,
                "mutators take fresh blocks only in the kept capacity"))
        {
            return;
        }
        *PayloadOf(third) = 0x3333;
        *PayloadOf(fourth) = 0x4444;
        roots.objects = {third, fourth};
        engine.RequestBorrow(64);
        RunPauseAndDrain(engine, CaptureMoving, &roots, "the pause that finds a free retained tail completes");
        size_t reused = (std::max)(BlockOfObject(engine, roots.objects[0]), BlockOfObject(engine, roots.objects[1]));
        Check(engine.GetNurseryMetrics().copyFailures == 0 && reused == 3 && engine.GetImmixCapacityBytes() == 4 * OvershootBlock &&
                *PayloadOf(roots.objects[0]) == 0x3333 && *PayloadOf(roots.objects[1]) == 0x4444,
            "the copy fills the free blocks of the retained tail from the lowest block, and the new borrow goes back");
        roots.objects.clear();
        RunPauseAndDrain(engine, CaptureMoving, &roots, "the pause that kills the second survivors completes");
        RunPauseAndDrain(engine, CaptureMoving, &roots, "the next pause completes");
        Check(engine.GetImmixCapacityBytes() == 2 * OvershootBlock && engine.GetBorrowedBytes() == 0 &&
                engine.GetOvershootMetric(LxrReferenceCounting::GiveBackComplete) == 1 &&
                !engine.IsDataOwnedForTest(arena + 2 * OvershootBlock) && engine.BlockCountersMatchWalk(),
            "when the survivors are dead the whole borrowed range goes back to the kept capacity");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "overshoot owner detaches");
    }

    // A pause with a large borrowed range: the decommit releases the memory and the planes of the free tail, and a
    // later pause that admits the same addresses finds zero planes.
    void ValidateOvershootPlanesRoundTrip()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        if (!InitializeOvershoot(engine, &roots, 2, 200, &mutator, &identity))
        {
            return;
        }
        Type big(24576, 0);
        Object* first = Allocate(engine, mutator, big);
        Object* second = Allocate(engine, mutator, big);
        if (first == nullptr || second == nullptr)
        {
            return;
        }
        *PayloadOf(first) = 0x3333;
        *PayloadOf(second) = 0x4444;
        roots.objects = {first, second};
        uintptr_t arena = engine.GetReservationStart();
        PauseWatch watch{&roots};
        watch.dirtyFirst = 20;
        watch.dirtyEnd = 130;
        watch.pattern = 0x5A;
        engine.RequestBorrow(1024);
        RunPauseAndDrain(engine, CaptureWatching, &watch, "the pause with 128 borrowed blocks completes");
        if (!Check(watch.capacity == 130 * OvershootBlock &&
                engine.GetImmixCapacityBytes() < 10 * OvershootBlock && engine.GetNurseryMetrics().copyFailures == 0,
                "128 blocks are admitted and the tail above the survivors goes back"))
        {
            return;
        }
        LxrSideMetadataLayout layout;
        LxrSideMetadataLayout::Create(3, &layout);
        uintptr_t tail = arena + 20 * OvershootBlock;
        size_t tailBytes = 110 * OvershootBlock;
        uintptr_t rcPlane = PlaneAddress(layout, LxrSideMetadataKind::ReferenceCount, tail);
        uintptr_t validPlane = PlaneAddress(layout, LxrSideMetadataKind::ValidObject, tail);
        int dataResident = ResidentPages(tail, tailBytes);
        int rcResident = ResidentPages(rcPlane, tailBytes / 8);
        int validResident = ResidentPages(validPlane, tailBytes / 64);
        Check(dataResident <= 0 && rcResident <= 0 && validResident <= 0,
            "the data and the planes of the returned range are no longer resident (Linux)");
        if (dataResident > 0 || rcResident > 0 || validResident > 0)
        {
            std::fprintf(stderr, "resident: data=%d rc=%d valid=%d\n", dataResident, rcResident, validResident);
        }
        Check(!engine.IsDataOwnedForTest(tail) && !engine.OwnsAddress(tail),
            "the returned range is not owned");
        Check(*PayloadOf(roots.objects[0]) == 0x3333 && *PayloadOf(roots.objects[1]) == 0x4444,
            "the survivors keep their payload");
        // The next pause admits the same addresses again. Every plane byte that it reads must be zero.
        Object* third = Allocate(engine, mutator, big);
        Object* fourth = Allocate(engine, mutator, big);
        if (third == nullptr || fourth == nullptr)
        {
            return;
        }
        *PayloadOf(third) = 0x5555;
        roots.objects.push_back(third);
        roots.objects.push_back(fourth);
        PauseWatch again{&roots};
        again.dirtyFirst = 20;
        again.dirtyEnd = 130;
        again.verifyZero = true;
        engine.RequestBorrow(1024);
        RunPauseAndDrain(engine, CaptureWatching, &again, "the pause that admits the same addresses again completes");
        Check(again.capacity >= 130 * OvershootBlock && again.nonZeroBytes == 0,
            "the planes of the re-admitted range are zero");
        Check(engine.IsOvershootEnabled() && engine.GetNurseryMetrics().copyFailures == 0 &&
                *PayloadOf(roots.objects[2]) == 0x5555 && *PayloadOf(roots.objects[0]) == 0x3333,
            "the copy into the re-admitted blocks keeps the payload");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "round trip owner detaches");
    }

    // Admission needs room in the reservation and the native planes; a refusal leaves the pause as it was.
    void ValidateOvershootRefusals()
    {
        {
            LxrReferenceCounting engine;
            LxrRcMutator* mutator = nullptr;
            MovingRoots roots;
            int identity;
            if (!InitializeOvershoot(engine, &roots, 2, 24, &mutator, &identity))
            {
                return;
            }
            Type big(24576, 0);
            Object* first = Allocate(engine, mutator, big);
            Object* second = Allocate(engine, mutator, big);
            roots.objects = {first, second};
            OvershootRefuse.store(true, std::memory_order_release);
            PauseWatch watch{&roots};
            engine.RequestBorrow(64);
            RunPauseAndDrain(engine, CaptureWatching, &watch, "the pause completes when the planes cannot grow");
            OvershootRefuse.store(false, std::memory_order_release);
            Check(watch.capacity == 2 * OvershootBlock && engine.GetImmixCapacityBytes() == 2 * OvershootBlock &&
                    engine.GetOvershootMetric(LxrReferenceCounting::BorrowRefused) == 1 &&
                    engine.GetOvershootMetric(LxrReferenceCounting::BorrowPauses) == 0 &&
                    engine.GetNurseryMetrics().copyFailures != 0 && roots.objects[0] == first,
                "a refused admission leaves the capacity and the copy as without overshoot");
            Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "refusal owner detaches");
        }
        {
            // The request is larger than the room that is left of the reservation: the pause takes what is left.
            LxrReferenceCounting engine;
            LxrRcMutator* mutator = nullptr;
            MovingRoots roots;
            int identity;
            if (!InitializeOvershoot(engine, &roots, 2, 6, &mutator, &identity))
            {
                return;
            }
            Type big(24576, 0);
            roots.objects = {Allocate(engine, mutator, big), Allocate(engine, mutator, big)};
            PauseWatch watch{&roots};
            engine.RequestBorrow(4096);
            RunPauseAndDrain(engine, CaptureWatching, &watch, "the pause completes when the request exceeds the reservation");
            Check(watch.capacity == 6 * OvershootBlock && engine.GetImmixCapacityBytes() <= 6 * OvershootBlock &&
                    engine.GetNurseryMetrics().copyFailures == 0,
                "the admission is bounded by the reservation");
            Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "bounded owner detaches");
        }
    }

    // With a hard limit, with a reservation equal to the capacity, or with the switch off, nothing changes.
    void ValidateOvershootHardLimit(int kind)
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        size_t hardLimit = kind == 0 ? 2 * OvershootBlock + 2 * LxrLargeObjectSpace::MinimumObjectBytes : 0;
        if (!InitializeOvershoot(engine, &roots, 2, kind == 1 ? 2 : 24, &mutator, &identity, hardLimit, kind != 2))
        {
            return;
        }
        Check(!engine.IsOvershootEnabled(), "a hard limit, a reservation equal to the capacity, or the switch disable overshoot");
        Type big(24576, 0);
        Object* first = Allocate(engine, mutator, big);
        Object* second = Allocate(engine, mutator, big);
        roots.objects = {first, second};
        PauseWatch watch{&roots};
        engine.RequestBorrow(64);
        RunPauseAndDrain(engine, CaptureWatching, &watch, "the pause completes");
        Check(watch.capacity == 2 * OvershootBlock && engine.GetImmixCapacityBytes() == 2 * OvershootBlock &&
                engine.GetNurseryMetrics().copyFailures != 0 && engine.GetNurseryMetrics().copiedBytes == 0 &&
                roots.objects[0] == first && roots.objects[1] == second &&
                engine.GetOvershootMetric(LxrReferenceCounting::BorrowPauses) == 0 &&
                engine.GetOvershootMetric(LxrReferenceCounting::GiveBackAttempts) == 0,
            "the capacity stays and the copy fails as before S5b");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "hard limit owner detaches");
    }

    // A regular growth is permanent: the borrowed range below it is kept, and the kept capacity rises.
    void ValidateOvershootRegularGrowth()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        if (!InitializeOvershoot(engine, &roots, 2, 24, &mutator, &identity))
        {
            return;
        }
        Type big(24576, 0);
        roots.objects = {Allocate(engine, mutator, big), Allocate(engine, mutator, big)};
        Check(engine.Grow(false, 24, CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.GetImmixCapacityBytes() > 2 * OvershootBlock &&
                engine.GetImmixFloorBytes() == engine.GetImmixCapacityBytes() && engine.GetBorrowedBytes() == 0,
            "a regular growth raises the kept capacity and leaves nothing borrowed");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "growth owner detaches");
    }

    // Random allocation, survival, death and pauses with borrowing. More than 127 pauses pass, so the epoch wraps and
    // resets the epoch plane of every owned range: a range that was given back must not be owned any more.
    void ValidateOvershootRandom(int kind)
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        if (!InitializeOvershoot(engine, &roots, 6, 80, &mutator, &identity, 0, true, kind == 1))
        {
            return;
        }
        std::vector<Type*> types;
        for (uint32_t size : {24u, 200u, 1000u, 4000u, 9000u, 20000u})
        {
            types.push_back(new Type(size, 0));
        }
        std::vector<uint64_t> tags;
        uint64_t state = 0x9E3779B97F4A7C15ull + static_cast<uint64_t>(kind);
        auto next = [&state]()
        {
            state ^= state << 13;
            state ^= state >> 7;
            state ^= state << 17;
            return state;
        };
        uintptr_t arena = engine.GetReservationStart();
        size_t floorBytes = engine.GetImmixFloorBytes();
        size_t reservation = 80 * OvershootBlock;
        int badTags = 0;
        int badCapacity = 0;
        int badCounters = 0;
        int badOwnership = 0;
        int rounds = 0;
        for (int round = 0; round < 160; round++)
        {
            size_t burst = 10 + next() % 40;
            for (size_t index = 0; index < burst; index++)
            {
                Object* object = TryAllocateQuietly(engine, mutator, *types[next() % types.size()]);
                if (object == nullptr)
                {
                    break;
                }
                uint64_t tag = next();
                *PayloadOf(object) = tag;
                if (next() % 100 < 25 && roots.objects.size() < 24)
                {
                    roots.objects.push_back(object);
                    tags.push_back(tag);
                }
            }
            for (size_t index = 0; index < roots.objects.size();)
            {
                if (next() % 100 < 30)
                {
                    roots.objects.erase(roots.objects.begin() + static_cast<std::ptrdiff_t>(index));
                    tags.erase(tags.begin() + static_cast<std::ptrdiff_t>(index));
                }
                else
                {
                    index++;
                }
            }
            engine.RequestBorrow(next() % 4 == 0 ? 0 : next() % 700);
            bool full = kind == 1 ? round % 4 == 3 : round % 11 == 10;
            LxrRcResult result = full ? engine.CollectFull(CaptureMoving, &roots, Timeout)
                : engine.Collect(CaptureMoving, &roots, Timeout);
            if (!Check(result == LxrRcResult::Success && engine.Drain(Timeout) == LxrRcResult::Success,
                    "random pause with borrowing completes"))
            {
                break;
            }
            rounds++;
            size_t capacity = engine.GetImmixCapacityBytes();
            badCapacity += capacity < floorBytes || capacity > reservation ||
                engine.GetBorrowedBytes() != capacity - floorBytes ? 1 : 0;
            badCounters += engine.BlockCountersMatchWalk() ? 0 : 1;
            badOwnership += engine.IsDataOwnedForTest(arena + capacity - 1) &&
                (capacity == reservation || !engine.IsDataOwnedForTest(arena + capacity)) ? 0 : 1;
            for (size_t index = 0; index < roots.objects.size(); index++)
            {
                badTags += *PayloadOf(roots.objects[index]) == tags[index] ? 0 : 1;
            }
        }
        Check(rounds == 160 && badCapacity == 0 && badCounters == 0 && badOwnership == 0 && badTags == 0,
            "capacity, counters, ownership and payloads hold in every random pause");
        Check(engine.GetOvershootMetric(LxrReferenceCounting::BorrowPauses) > 20 &&
                engine.GetOvershootMetric(LxrReferenceCounting::GiveBackSuccesses) > 20 &&
                engine.GetMetrics().epochs >= 160,
            "the random run borrowed and gave back many times, across the epoch wrap");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "random overshoot owner detaches");
        for (Type* type : types)
        {
            delete type;
        }
    }

    void ValidateS5bOvershoot()
    {
        ValidateOvershootPolicy();
        ValidateOvershootBorrowAndGiveBack();
        ValidateOvershootPlanesRoundTrip();
        ValidateOvershootRefusals();
        for (int kind = 0; kind < 3; kind++)
        {
            ValidateOvershootHardLimit(kind);
        }
        ValidateOvershootRegularGrowth();
        for (int kind = 0; kind < 2; kind++)
        {
            ValidateOvershootRandom(kind);
        }
    }

    // ---- P17 S6: the finalizer registry and the retained roots ----

    std::vector<Object*> S6Suppressed;
    std::vector<Object*> S6Eager;

    bool S6IsSuppressed(Object* object)
    {
        return std::find(S6Suppressed.begin(), S6Suppressed.end(), object) != S6Suppressed.end();
    }

    bool S6IsEager(Object* object)
    {
        return std::find(S6Eager.begin(), S6Eager.end(), object) != S6Eager.end();
    }

    // The engine with the registry glue of the collector: the root capture, the decision of a tracing pause and the
    // relocation of the entries are the functions of lxr_finalizer_pause.h that the collector calls.
    struct S6Harness
    {
        LxrReferenceCounting engine;
        LxrFinalizerRegistry registry;
        LxrFinalizerPauseCounters counters;
        std::vector<Object*> roots;
        // Roots that only get an increment, as the targets of weak handles do in a reference-counting pause.
        std::vector<Object*> weakRoots;
        LxrPauseKind kind = LxrPauseKind::RefCount;
        bool evacuateMature = false;
        LxrRcMutator* mutator = nullptr;
        int identity = 0;

        static LxrRcResult Capture(LxrReferenceCounting*, void* context)
        {
            S6Harness* harness = static_cast<S6Harness*>(context);
            LxrRcResult result = LxrFinalizerCapture(
                harness->engine, harness->registry, harness->kind, S6IsSuppressed, &harness->counters);
            for (Object* root : harness->roots)
            {
                if (result == LxrRcResult::Success)
                {
                    result = harness->engine.AddRoot(root);
                }
            }
            for (Object* root : harness->weakRoots)
            {
                if (result == LxrRcResult::Success)
                {
                    result = harness->engine.AddRoot(root, false, false);
                }
            }
            return result;
        }

        static LxrRcResult Decide(LxrReferenceCounting*, void* context)
        {
            S6Harness* harness = static_cast<S6Harness*>(context);
            return LxrFinalizerDecide(
                harness->engine, harness->registry, S6IsSuppressed, S6IsEager, &harness->counters);
        }

        static LxrRcResult Relocate(LxrReferenceCounting*, void* context)
        {
            S6Harness* harness = static_cast<S6Harness*>(context);
            LxrFinalizerRelocate(harness->engine, harness->registry, harness->evacuateMature, harness->kind);
            LxrRcResult result = LxrRcResult::Success;
            for (Object*& root : harness->roots)
            {
                if (result == LxrRcResult::Success)
                {
                    result = harness->engine.RelocateRoot(&root, false, false);
                }
            }
            for (Object*& root : harness->weakRoots)
            {
                if (result == LxrRcResult::Success)
                {
                    result = harness->engine.RelocateRoot(&root, false, false);
                }
            }
            return result;
        }

        bool Initialize(bool nursery)
        {
            LxrRcConfiguration configuration{
                16 * ImmixBlockGeometry::BlockBytes, 2 * LxrLargeObjectSpace::MinimumObjectBytes, 4, 4096, 4, 4,
            };
            configuration.evacuateNursery = nursery;
            configuration.lineRecycling = false;
            LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
            LxrRcObjectModel model{};
            model.context = this;
            model.finishTrace = Decide;
            model.finishRelocation = Relocate;
            S6Suppressed.clear();
            S6Eager.clear();
            return Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success,
                    "S6 engine initializes") &&
                Check(engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success, "S6 mutator attaches");
        }

        bool Pause(LxrPauseKind pause)
        {
            kind = pause;
            LxrRcResult result = pause == LxrPauseKind::Full ? engine.CollectFull(Capture, this, Timeout) :
                pause == LxrPauseKind::InitialMark ? engine.BeginTrace(Capture, this, Timeout) :
                pause == LxrPauseKind::FinalMark ? engine.FinishTrace(Capture, this, Timeout) :
                engine.Collect(Capture, this, Timeout);
            return Check(result == LxrRcResult::Success && engine.Drain(Timeout) == LxrRcResult::Success,
                "S6 pause and its lazy tail complete");
        }

        std::vector<Object*> Counted() const
        {
            std::vector<Object*> objects;
            registry.ForEachCounted([&](Object* object, bool) { objects.push_back(object); });
            return objects;
        }

        std::vector<Object*> Ready() const
        {
            std::vector<Object*> objects;
            registry.ForEachReady([&](Object* object, bool) { objects.push_back(object); });
            return objects;
        }
    };

    bool S6Contains(const std::vector<Object*>& objects, Object* object)
    {
        return std::find(objects.begin(), objects.end(), object) != objects.end();
    }

    // Many threads append at the same time. Every value comes out once; a full chunk is replaced without loss.
    void ValidateS6ConcurrentAppend()
    {
        constexpr size_t Threads = 8;
        constexpr size_t PerThread = 40000;
        LxrAppendList list;
        std::vector<std::thread> threads;
        std::atomic<bool> failed{false};
        for (size_t thread = 0; thread < Threads; thread++)
        {
            threads.emplace_back([&, thread]()
            {
                for (size_t index = 0; index < PerThread; index++)
                {
                    if (!list.Append((thread * PerThread + index + 1) * 8))
                    {
                        failed = true;
                    }
                }
            });
        }
        for (std::thread& thread : threads)
        {
            thread.join();
        }
        std::vector<uint8_t> seen(Threads * PerThread + 1, 0);
        size_t duplicates = 0;
        size_t outOfRange = 0;
        size_t total = list.Drain([&](uintptr_t value)
        {
            size_t slot = value / 8;
            if (slot == 0 || slot >= seen.size())
            {
                outOfRange++;
            }
            else if (seen[slot]++ != 0)
            {
                duplicates++;
            }
        });
        size_t missing = 0;
        for (size_t slot = 1; slot < seen.size(); slot++)
        {
            missing += seen[slot] == 0;
        }
        Check(!failed && total == Threads * PerThread && duplicates == 0 && outOfRange == 0 && missing == 0,
            "concurrent appends from many threads are all kept exactly once across many chunks");
        Check(list.IsEmpty() && list.Drain([](uintptr_t) {}) == 0, "a drained list is empty");
        Check(list.Append(8) && list.Drain([](uintptr_t) {}) == 1, "a drained list takes new values");
    }

    // The registry states on fake object addresses: new entries, suppressed entries, counts, decisions, duplicates,
    // the order of the finalizer queue and the relocation of entries. Nothing here reads an object.
    void ValidateS6RegistryStates()
    {
        auto fake = [](size_t index) { return reinterpret_cast<Object*>(0x100000 + index * 64); };
        LxrFinalizerRegistry registry;
        std::vector<Object*> retained;
        std::vector<Object*> marked;
        std::vector<Object*> suppressed;
        auto isSuppressed = [&](Object* object) { return S6Contains(suppressed, object); };
        auto isMarked = [&](Object* object) { return S6Contains(marked, object); };
        auto never = [](Object*) { return false; };
        for (size_t index = 0; index < 8; index++)
        {
            Check(registry.Register(fake(index), index == 6), "registration appends");
        }
        suppressed = {fake(1), fake(2)};
        Check(registry.CaptureNew(isSuppressed, [&](Object* object) { retained.push_back(object); }),
            "capture takes the new entries");
        Check(retained.size() == 6 && !S6Contains(retained, fake(1)) && !S6Contains(retained, fake(2)) &&
                registry.GetCountedEntries() == 6 && registry.GetDropped() == 2,
            "a suppressed new entry is dropped without a count; every other entry gets one");
        Check(registry.GetPendingCount() == 0 && !registry.HasNewEntries(),
            "an entry that is only counted is not ready, and the new list is empty");
        // Nothing is marked yet in a reference-counting pause: no decision is made there.
        Check(registry.GetPendingCount() == 0, "a pause without a decision queues nothing");
        // A tracing pause: 0 and 3 are marked, 4 is suppressed after its count, 5 is eager, 6 (critical) and 7 are dead.
        marked = {fake(0), fake(3)};
        suppressed.push_back(fake(4));
        std::vector<Object*> dropped;
        bool decided = registry.Decide([](Object* object) { return object; }, isSuppressed, isMarked,
            [&](Object* object) { return object == fake(5); }, [&](Object* object) { dropped.push_back(object); });
        Check(decided && registry.GetPendingCount() == 2 && registry.GetCountedEntries() == 2 &&
                dropped.size() == 2 && S6Contains(dropped, fake(4)) && S6Contains(dropped, fake(5)),
            "a decision moves unmarked entries to the ready list, keeps marked entries, drops suppressed and eager ones");
        std::vector<Object*> traced;
        registry.ForEachNewReady([&](Object* object) { traced.push_back(object); });
        Check(traced.size() == 2 && S6Contains(traced, fake(6)) && S6Contains(traced, fake(7)),
            "the new ready entries are handed out for tracing");
        traced.clear();
        registry.ForEachNewReady([&](Object* object) { traced.push_back(object); });
        Check(traced.empty(), "a ready entry is handed out for tracing once");
        std::vector<Object*> released;
        registry.DrainReleased([&](Object* object) { released.push_back(object); });
        Check(released.size() == 2 && S6Contains(released, fake(4)) && S6Contains(released, fake(5)),
            "a dropped entry gives its count back");
        // The finalizer thread takes the normal entry first and the critical one last; it lets go of each count.
        suppressed.push_back(fake(7));
        Object* first = registry.TakeNext(isSuppressed);
        Object* second = registry.TakeNext(isSuppressed);
        Check(first == fake(6) && second == nullptr && registry.GetPendingCount() == 0,
            "a ready entry whose finalization was suppressed after the decision is skipped; the critical one comes last");
        released.clear();
        registry.DrainReleased([&](Object* object) { released.push_back(object); });
        Check(released.size() == 2 && S6Contains(released, fake(7)) && S6Contains(released, fake(6)),
            "the finalizer thread lets go of the count of each entry it takes or skips");
        // Duplicates: finalization suppressed and registered again before a pause looked at the object, or two
        // registrations. Both entries reach one decision: one ready entry, one count given back.
        marked.clear();
        suppressed.clear();
        LxrFinalizerRegistry duplicates;
        duplicates.Register(fake(20), false);
        duplicates.Register(fake(20), true);
        duplicates.Register(fake(21), false);
        duplicates.CaptureNew(isSuppressed, [](Object*) {});
        duplicates.Decide([](Object* object) { return object; }, isSuppressed, isMarked, never, [](Object*) {});
        released.clear();
        duplicates.DrainReleased([&](Object* object) { released.push_back(object); });
        Check(duplicates.GetPendingCount() == 2 && released.size() == 1 && released[0] == fake(20),
            "two entries of one object give one ready entry and give one count back");
        Check(duplicates.TakeNext(isSuppressed) == fake(21) && duplicates.TakeNext(isSuppressed) == fake(20) &&
                duplicates.TakeNext(isSuppressed) == nullptr,
            "the merged entry keeps the critical bit of its entries");
        // Re-registration is idempotent. New entries of one object are merged in the capture (they have no count to
        // give back); a new entry next to a counted one is merged in the next decision, and the extra count is given
        // back. Otherwise a pooled object that suppresses and re-registers itself would collect entries and counts.
        marked.clear();
        suppressed.clear();
        LxrFinalizerRegistry pooled;
        std::vector<Object*> pooledRetained;
        pooled.Register(fake(40), false);
        pooled.Register(fake(40), false, true);
        pooled.Register(fake(40), false, true);
        pooled.CaptureNew(isSuppressed, [&](Object* object) { pooledRetained.push_back(object); });
        Check(pooledRetained.size() == 1 && pooled.GetCountedEntries() == 1,
            "new entries of one re-registered object give one counted entry");
        pooled.Register(fake(40), false, true);
        pooled.CaptureNew(isSuppressed, [&](Object* object) { pooledRetained.push_back(object); });
        Check(pooled.GetCountedEntries() == 2 && pooledRetained.size() == 2,
            "a re-registration after a pause adds a counted entry until the next decision");
        marked = {fake(40)};
        pooled.Decide([](Object* object) { return object; }, isSuppressed, isMarked, never, [](Object*) {});
        released.clear();
        pooled.DrainReleased([&](Object* object) { released.push_back(object); });
        Check(pooled.GetCountedEntries() == 1 && pooled.GetPendingCount() == 0 && released.size() == 1 &&
                released[0] == fake(40),
            "the decision merges the counted entries of a live object and gives the extra count back");
        // A count that a decision gives up is an address too: a mature copy in the same pause can move the object.
        LxrFinalizerRegistry held;
        held.Register(fake(50), false);
        held.CaptureNew(isSuppressed, [](Object*) {});
        suppressed = {fake(50)};
        marked.clear();
        held.Decide([](Object* object) { return object; }, isSuppressed, isMarked, never, [](Object*) {});
        auto moveAhead = [](Object* object) { return reinterpret_cast<Object*>(reinterpret_cast<uintptr_t>(object) + 0x2000000); };
        held.Relocate(true, moveAhead);
        released.clear();
        held.DrainReleased([&](Object* object) { released.push_back(object); });
        Check(released.size() == 1 && released[0] == moveAhead(fake(50)),
            "the count that a decision gives up follows an object that a copy in the same pause moved");
        suppressed.clear();
        // Relocation: only the current batch, or every entry.
        LxrFinalizerRegistry moving;
        auto relocate = [](Object* object) { return reinterpret_cast<Object*>(reinterpret_cast<uintptr_t>(object) + 0x1000000); };
        moving.Register(fake(30), false);
        moving.CaptureNew(isSuppressed, [](Object*) {});
        moving.Register(fake(31), false);
        moving.CaptureNew(isSuppressed, [](Object*) {});
        moving.Relocate(false, relocate);
        std::vector<Object*> counted;
        moving.ForEachCounted([&](Object* object, bool) { counted.push_back(object); });
        Check(counted.size() == 2 && counted[0] == fake(30) && counted[1] == relocate(fake(31)),
            "relocation of the current batch leaves the older entries alone");
        moving.Relocate(true, relocate);
        counted.clear();
        moving.ForEachCounted([&](Object* object, bool) { counted.push_back(object); });
        Check(counted.size() == 2 && counted[0] == relocate(fake(30)) && counted[1] == relocate(relocate(fake(31))),
            "relocation of every entry moves each entry");
        // Registration from many threads: every object is counted exactly once.
        LxrFinalizerRegistry many;
        std::vector<std::thread> threads;
        for (size_t thread = 0; thread < 8; thread++)
        {
            threads.emplace_back([&, thread]()
            {
                for (size_t index = 0; index < 20000; index++)
                {
                    many.Register(fake(1000 + thread * 20000 + index), false);
                }
            });
        }
        for (std::thread& thread : threads)
        {
            thread.join();
        }
        std::vector<uint8_t> seen(8 * 20000, 0);
        size_t bad = 0;
        many.CaptureNew(isSuppressed, [&](Object* object)
        {
            size_t slot = (reinterpret_cast<uintptr_t>(object) - 0x100000) / 64 - 1000;
            if (slot >= seen.size() || seen[slot]++ != 0)
            {
                bad++;
            }
        });
        Check(bad == 0 && many.GetCountedEntries() == seen.size() && many.GetRegistered() == seen.size(),
            "concurrent registrations are counted exactly once");
    }

    // A reference-counting pause keeps every registered object alive, young or old, and decides nothing.
    void ValidateS6RcPause(bool nursery)
    {
        S6Harness harness;
        if (!harness.Initialize(nursery))
        {
            return;
        }
        Type leaf(24, 0);
        Object* dead = Allocate(harness.engine, harness.mutator, leaf);
        Object* kept = Allocate(harness.engine, harness.mutator, leaf);
        Object* plain = Allocate(harness.engine, harness.mutator, leaf);
        Object* old = Allocate(harness.engine, harness.mutator, leaf);
        if (dead == nullptr || kept == nullptr || plain == nullptr || old == nullptr)
        {
            return;
        }
        harness.registry.Register(dead, false);
        harness.registry.Register(kept, false);
        harness.roots = {kept, old};
        if (!harness.Pause(LxrPauseKind::RefCount))
        {
            return;
        }
        std::vector<Object*> counted = harness.Counted();
        bool allCounted = counted.size() == 2;
        for (Object* object : counted)
        {
            allCounted = allCounted && harness.engine.IsObjectStart(object) && Count(harness.engine, object) > 0;
        }
        Check(allCounted && harness.registry.GetPendingCount() == 0,
            "a registered young object that nothing else references is promoted and survives a reference-counting pause");
        Check(Count(harness.engine, plain) == 0, "an unregistered young object that nothing references dies");
        Check(harness.counters.retained.load() == 2, "the root capture retained exactly the two new entries");
        // An old object (counted by a root) that is registered later gets one more count at the next pause.
        uintptr_t before = Count(harness.engine, harness.roots[1]);
        Check(before != 0, "the old object was promoted by its root");
        harness.registry.Register(harness.roots[1], false);
        harness.roots = {harness.roots[1]};
        if (!harness.Pause(LxrPauseKind::RefCount))
        {
            return;
        }
        Check(Count(harness.engine, harness.roots[0]) == before + 1,
            "registering an old object adds one count at the next pause");
        for (int round = 0; round < 3; round++)
        {
            harness.roots.clear();
            if (!harness.Pause(LxrPauseKind::RefCount))
            {
                return;
            }
        }
        counted = harness.Counted();
        bool alive = counted.size() == 3;
        for (Object* object : counted)
        {
            alive = alive && Count(harness.engine, object) > 0;
        }
        Check(alive && harness.registry.GetPendingCount() == 0,
            "registered objects stay alive, and no reference-counting pause moves one to the ready list");
        // The count that the root capture gave stays: the weak-handle rule of the collector is the same increment
        // without a trace (AddRoot with trace=false), so a weak target survives these pauses too.
        harness.roots.clear();
        Object* weakTarget = Allocate(harness.engine, harness.mutator, leaf);
        if (weakTarget == nullptr)
        {
            return;
        }
        harness.weakRoots = {weakTarget};
        if (!harness.Pause(LxrPauseKind::RefCount))
        {
            return;
        }
        Check(Count(harness.engine, harness.weakRoots[0]) > 0 && harness.engine.IsObjectStart(harness.weakRoots[0]),
            "the target of a weak handle gets its increment in a reference-counting pause and survives it");
        Check(harness.engine.DetachMutator(harness.mutator) == LxrRcResult::Success, "S6 mutator detaches");
    }

    // A tracing pause decides: an unmarked registered object is queued and traced with its children, a marked one
    // stays, a suppressed or eagerly finalized one is dropped. The finalizer thread takes the queued object; the next
    // pause takes its count away and the object dies by reference counting.
    void ValidateS6TracingDecision()
    {
        S6Harness harness;
        if (!harness.Initialize(false))
        {
            return;
        }
        Type leaf(24, 0);
        Type link(24, 1);
        Object* dead = Allocate(harness.engine, harness.mutator, link);
        Object* child = Allocate(harness.engine, harness.mutator, leaf);
        Object* rooted = Allocate(harness.engine, harness.mutator, leaf);
        Object* suppressedLater = Allocate(harness.engine, harness.mutator, leaf);
        Object* suppressedEarly = Allocate(harness.engine, harness.mutator, leaf);
        Object* eager = Allocate(harness.engine, harness.mutator, leaf);
        if (dead == nullptr || child == nullptr || rooted == nullptr || suppressedLater == nullptr ||
            suppressedEarly == nullptr || eager == nullptr || !Store(harness.engine, harness.mutator, Field(dead), child))
        {
            return;
        }
        for (Object* object : {dead, rooted, suppressedLater, suppressedEarly, eager})
        {
            harness.registry.Register(object, false);
        }
        S6Suppressed = {suppressedEarly};
        harness.roots = {rooted};
        if (!harness.Pause(LxrPauseKind::RefCount))
        {
            return;
        }
        Check(harness.Counted().size() == 4 && harness.registry.GetDropped() == 1 && harness.registry.GetPendingCount() == 0,
            "the first pause counts four entries, drops the suppressed one and queues nothing");
        S6Suppressed.push_back(suppressedLater);
        S6Eager = {eager};
        if (!harness.Pause(LxrPauseKind::Full))
        {
            return;
        }
        std::vector<Object*> ready = harness.Ready();
        Check(ready.size() == 1 && ready[0] == dead && harness.registry.GetPendingCount() == 1,
            "a full pause queues exactly the unmarked registered object");
        Check(harness.engine.IsMarked(dead) && harness.engine.IsMarked(child) && Count(harness.engine, child) > 0,
            "the queued object is traced with its children, so the children survive (f-reachability)");
        Check(harness.engine.IsMarked(rooted) && S6Contains(harness.Counted(), rooted) && harness.Counted().size() == 1,
            "a marked registered object stays in the registry, and no other entry stays");
        Check(harness.engine.IsMarked(suppressedLater) && harness.engine.IsMarked(eager),
            "a dropped entry is traced, so the cycle sweep cannot free its object before the count is taken away");
        Check(harness.registry.GetDropped() == 3, "the suppressed (new and counted) and the eager entries are dropped");
        Object* taken = harness.registry.TakeNext(S6IsSuppressed);
        Check(taken == dead && harness.registry.GetPendingCount() == 0,
            "the finalizer thread takes the queued object");
        harness.roots.clear();
        if (!harness.Pause(LxrPauseKind::RefCount))
        {
            return;
        }
        Check(Count(harness.engine, dead) == 0 && Count(harness.engine, child) == 0 &&
                Count(harness.engine, suppressedLater) == 0 && Count(harness.engine, eager) == 0,
            "the next pause takes the counts away, and the objects and their children die by reference counting");
        Check(harness.engine.DetachMutator(harness.mutator) == LxrRcResult::Success, "S6 mutator detaches");
    }

    // The cycle sweep of a tracing pause frees dead cycles. It must not free a cycle that holds a registered object:
    // the decision queues the object and traces it, so the whole cycle is marked.
    void ValidateS6CycleSweepSkipsRegistered()
    {
        S6Harness harness;
        if (!harness.Initialize(false))
        {
            return;
        }
        Type link(24, 1);
        Object* registeredX = Allocate(harness.engine, harness.mutator, link);
        Object* registeredY = Allocate(harness.engine, harness.mutator, link);
        Object* plainZ = Allocate(harness.engine, harness.mutator, link);
        Object* plainW = Allocate(harness.engine, harness.mutator, link);
        if (registeredX == nullptr || registeredY == nullptr || plainZ == nullptr || plainW == nullptr ||
            !Store(harness.engine, harness.mutator, Field(registeredX), registeredY) ||
            !Store(harness.engine, harness.mutator, Field(registeredY), registeredX) ||
            !Store(harness.engine, harness.mutator, Field(plainZ), plainW) ||
            !Store(harness.engine, harness.mutator, Field(plainW), plainZ))
        {
            return;
        }
        harness.registry.Register(registeredX, false);
        harness.roots = {registeredY, plainZ};
        if (!harness.Pause(LxrPauseKind::RefCount))
        {
            return;
        }
        harness.roots.clear();
        if (!harness.Pause(LxrPauseKind::RefCount))
        {
            return;
        }
        Check(Count(harness.engine, registeredX) > 0 && Count(harness.engine, registeredY) > 0 &&
                Count(harness.engine, plainZ) > 0 && Count(harness.engine, plainW) > 0,
            "both cycles are garbage that reference counting cannot free");
        if (!harness.Pause(LxrPauseKind::Full))
        {
            return;
        }
        Check(Count(harness.engine, plainZ) == 0 && Count(harness.engine, plainW) == 0,
            "the cycle sweep frees the dead cycle that holds no registered object");
        Check(Count(harness.engine, registeredX) > 0 && Count(harness.engine, registeredY) > 0 &&
                harness.engine.IsMarked(registeredX) && harness.engine.IsMarked(registeredY) &&
                harness.registry.GetPendingCount() == 1,
            "the cycle sweep skips the cycle of a registered object: it is queued and marked");
        Check(harness.engine.DetachMutator(harness.mutator) == LxrRcResult::Success, "S6 mutator detaches");
    }

    // A nursery copy moves a registered young object in the pause that counts it. The entry follows the object.
    void ValidateS6Relocation()
    {
        S6Harness harness;
        if (!harness.Initialize(true))
        {
            return;
        }
        Type leaf(64, 0);
        std::vector<Object*> originals;
        for (size_t index = 0; index < 32; index++)
        {
            Object* object = Allocate(harness.engine, harness.mutator, leaf);
            if (object == nullptr)
            {
                return;
            }
            reinterpret_cast<uint8_t*>(object)[32] = static_cast<uint8_t>(index + 1);
            harness.registry.Register(object, false);
            originals.push_back(object);
        }
        if (!harness.Pause(LxrPauseKind::RefCount))
        {
            return;
        }
        std::vector<Object*> counted = harness.Counted();
        size_t moved = 0;
        size_t intact = 0;
        for (size_t index = 0; index < counted.size(); index++)
        {
            Object* object = counted[index];
            moved += object != originals[index];
            intact += harness.engine.IsObjectStart(object) && Count(harness.engine, object) > 0 &&
                reinterpret_cast<uint8_t*>(object)[32] == index + 1;
        }
        Check(counted.size() == 32 && harness.engine.GetNurseryMetrics().copiedObjects >= 32 && moved >= 16 &&
                intact == 32,
            "a nursery copy moves registered objects, and each entry follows its object with its payload");
        if (!harness.Pause(LxrPauseKind::Full))
        {
            return;
        }
        std::vector<Object*> ready = harness.Ready();
        size_t valid = 0;
        for (Object* object : ready)
        {
            valid += harness.engine.IsObjectStart(object) && Count(harness.engine, object) > 0 &&
                harness.engine.IsMarked(object);
        }
        Check(ready.size() == 32 && valid == 32, "the entries that the full pause queues name valid, marked objects");
        Check(harness.engine.DetachMutator(harness.mutator) == LxrRcResult::Success, "S6 mutator detaches");
    }

    // The list of objects whose pin or copy flag was set in a pause: the pause clears the flags from the list.
    void ValidateS6TouchedList()
    {
        constexpr size_t Objects = 8 * 5000;
        std::vector<std::atomic<uint32_t>> flags(Objects);
        for (std::atomic<uint32_t>& flag : flags)
        {
            flag.store(0x80);
        }
        LxrAppendList touched;
        std::vector<std::thread> threads;
        for (size_t thread = 0; thread < 8; thread++)
        {
            threads.emplace_back([&, thread]()
            {
                for (size_t index = thread * 5000; index < (thread + 1) * 5000; index++)
                {
                    if (index % 3 != 0)
                    {
                        flags[index].fetch_or(0x300);
                        touched.Append((index + 1) * 8);
                    }
                }
            });
        }
        for (std::thread& thread : threads)
        {
            thread.join();
        }
        size_t expected = 0;
        for (size_t index = 0; index < Objects; index++)
        {
            expected += index % 3 != 0;
        }
        size_t cleared = touched.Drain([&](uintptr_t value)
        {
            flags[value / 8 - 1].fetch_and(~uint32_t{0x300});
        });
        size_t left = 0;
        size_t lost = 0;
        for (size_t index = 0; index < Objects; index++)
        {
            left += (flags[index].load() & 0x300) != 0;
            lost += (flags[index].load() & 0x80) == 0;
        }
        Check(cleared == expected && left == 0 && lost == 0,
            "the pause clears the pin and copy flags of exactly the objects on the touched list and no other flag");
        Check(touched.Drain([](uintptr_t) {}) == 0, "the touched list is empty after the pause");
    }

    void ValidateS6Registry()
    {
        ValidateS6ConcurrentAppend();
        ValidateS6RegistryStates();
        ValidateS6RcPause(false);
        ValidateS6RcPause(true);
        ValidateS6TracingDecision();
        ValidateS6CycleSweepSkipsRegistered();
        ValidateS6Relocation();
        ValidateS6TouchedList();
    }

    // ---- P17 S7: the pin list, the pause bit of a block and the pinned blocks. The collector keeps no pin flag
    // for each object any more. ----

    Object* AllocatePinnedObject(LxrReferenceCounting& engine, LxrRcMutator* mutator, Type& type)
    {
        Object* object = nullptr;
        if (!Check(engine.Allocate(mutator, type.table->GetBaseSize(), &object, true) == LxrRcResult::Success,
                "pinned-object-heap allocation succeeds"))
        {
            return nullptr;
        }
        object->RawSetMethodTable(type.table);
        return object;
    }

    void ValidateS7PinListUnit()
    {
        LxrPinList list;
        Check(list.IsEmpty() && list.Seal() == 0 && list.Size() == 0 && !list.Contains(0x1000),
            "an empty pin list seals to no entry");
        list.Clear();
        const uintptr_t values[] = {0x4000, 0x1000, 0x1000, 0x2008, 0x4000, 0x3010};
        for (size_t index = 0; index < 6; index++)
        {
            list.Add(index % 3, values[index]);
        }
        Check(list.GetAdded() == 6 && !list.IsEmpty(), "every append is counted, duplicates included");
        size_t unique = list.Seal();
        Check(unique == 4 && list.Size() == 4 && list[0] == 0x1000 && list[1] == 0x2008 && list[2] == 0x3010 &&
                list[3] == 0x4000 && list.IsSealed(),
            "the merge of the shards is sorted and has no duplicate");
        Check(list.Contains(0x2008) && list.Contains(0x1000) && !list.Contains(0x2000) && !list.Contains(0x5000),
            "the lookup finds exactly the entries");
        list.Clear();
        Check(list.IsEmpty() && list.Size() == 0 && !list.Contains(0x1000) && !list.IsSealed() &&
                list.GetAdded() == 0,
            "clear empties the list at the end of the pause");
        list.Add(2, 0x8000);
        Check(list.Seal() == 1 && list.Contains(0x8000) && !list.Contains(0x1000),
            "the list works again after clear and keeps nothing of the earlier pause");
        list.Clear();
    }

    void ValidateS7PinListConcurrent()
    {
        constexpr size_t Threads = 8;
        constexpr size_t PerThread = 20000;
        LxrPinList list;
        std::vector<std::thread> threads;
        for (size_t thread = 0; thread < Threads; thread++)
        {
            threads.emplace_back([&, thread]()
            {
                for (size_t index = 0; index < PerThread; index++)
                {
                    list.Add(thread, (thread * PerThread + index + 1) * 8);
                }
                for (size_t index = 0; index < 100; index++)
                {
                    list.Add(thread, (index + 1) * 8);
                }
            });
        }
        for (std::thread& thread : threads)
        {
            thread.join();
        }
        size_t unique = list.Seal();
        bool sorted = true;
        for (size_t index = 1; index < list.Size(); index++)
        {
            sorted = sorted && list[index - 1] < list[index];
        }
        size_t missing = 0;
        for (size_t value = 1; value <= Threads * PerThread; value += 7)
        {
            missing += list.Contains(value * 8) ? 0 : 1;
        }
        Check(list.GetAdded() == Threads * (PerThread + 100) && unique == Threads * PerThread && sorted && missing == 0,
            "eight workers add at the same time with their own shards: no entry is lost, the merge has no duplicate");
        list.Clear();
    }

    void ValidateS7NurseryPin(uint8_t bits)
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        if (!InitializeMoving(engine, &roots, 16, &mutator, &identity, bits))
        {
            return;
        }
        Type little(64, 0);
        Object* pinned = Allocate(engine, mutator, little);
        Object* neighbour = Allocate(engine, mutator, little);
        Object* other = Allocate(engine, mutator, little);
        if (pinned == nullptr || neighbour == nullptr || other == nullptr)
        {
            return;
        }
        size_t block = BlockOfObject(engine, pinned);
        Check(BlockOfObject(engine, neighbour) == block && BlockOfObject(engine, other) == block,
            "the pinned object and its neighbours lie in one nursery block");
        // The pause promotes a block in place when it reaches a pinned object, and objects that it reaches later in
        // that block stay. The root order puts the pinned object last, so the neighbours are reached first.
        roots.objects = {neighbour, other, pinned};
        roots.pinned = pinned;
        roots.probe = true;
        roots.probeNeighbour = neighbour;
        Check(engine.CountPausePinnedBlocks() == 0, "no block has the pin bit before the pause");
        Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
            "a pause with a pinned root completes");
        Check(roots.probeBlocks == 1 && roots.probePinned && !roots.probeNeighbourPinned,
            "inside the pause exactly one block has the pin bit; the list holds the pinned object and not its neighbour");
        Check(roots.objects[2] == pinned, "the pinned object in a nursery block is not copied");
        Check(roots.objects[0] != neighbour && roots.objects[1] != other,
            "the unpinned neighbours in the block of the pin are copied");
        LxrNurseryMetrics metrics = engine.GetNurseryMetrics();
        Check(metrics.copiedObjects == 2 && metrics.inPlacePinnedBytes == 64,
            "the pause counts the pinned object as in-place and the neighbours as copied");
        Check(BlockStateOf(engine, block) != ImmixBlockState::Unallocated && engine.IsObjectStart(pinned),
            "the block of the pin is promoted in place");
        Check(engine.CountPausePinnedBlocks() == 0 && !engine.IsPinnedForPause(pinned) &&
                engine.GetPinnedRootCount() == 1,
            "the pin bit is clear at the end of the pause, and the pause counted one pinned root");
        // A pause without a pin sets no bit and copies what is young.
        Object* young = Allocate(engine, mutator, little);
        roots.pinned = nullptr;
        roots.probeNeighbour = nullptr;
        roots.objects = {roots.objects[2], young};
        Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success && roots.probeBlocks == 0 && roots.objects[1] != young &&
                engine.GetPinnedRootCount() == 0,
            "a pause without a pinned root sets no pin bit and copies the young object");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "S7 nursery mutator detaches");
    }

    void ValidateS7InteriorPin(uint8_t bits)
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        if (!InitializeMoving(engine, &roots, 16, &mutator, &identity, bits))
        {
            return;
        }
        Type body(512, 0);
        Object* pinned = Allocate(engine, mutator, body);
        Object* neighbour = Allocate(engine, mutator, body);
        if (pinned == nullptr || neighbour == nullptr)
        {
            return;
        }
        roots.objects = {neighbour};
        roots.interior = reinterpret_cast<Object*>(reinterpret_cast<uint8_t*>(pinned) + 200);
        roots.pinInterior = true;
        Object* interior = roots.interior;
        Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
            "a pause with an interior pinned root completes");
        Check(roots.interior == interior && engine.IsObjectStart(pinned),
            "an interior pointer into a pinned object keeps the whole object where it is");
        Check(roots.objects[0] != neighbour && engine.GetNurseryMetrics().inPlacePinnedBytes == 512,
            "the object that the interior pointer names is the pinned one, and the neighbour is copied");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "S7 interior mutator detaches");
    }

    void ValidateS7ConcurrentPins(uint8_t bits)
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        if (!InitializeMoving(engine, &roots, 32, &mutator, &identity, bits))
        {
            return;
        }
        Type body(1000, 0);
        std::vector<Object*> before;
        for (size_t index = 0; index < 300; index++)
        {
            Object* object = Allocate(engine, mutator, body);
            if (object == nullptr)
            {
                return;
            }
            before.push_back(object);
        }
        // Movable objects first in the root order: a pinned object promotes its block in place, so objects that the
        // pause reaches later in the same block stay.
        roots.objects.assign(before.begin() + 200, before.end());
        roots.objects.insert(roots.objects.end(), before.begin(), before.begin() + 200);
        roots.pinThreadObjects.assign(before.begin(), before.begin() + 200);
        roots.pinThreads = 4;
        Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
            "a pause with pins from four workers completes");
        size_t stayed = 0;
        size_t moved = 0;
        for (size_t index = 0; index < before.size(); index++)
        {
            moved += index < 100 && roots.objects[index] != before[200 + index];
            stayed += index >= 100 && roots.objects[index] == before[index - 100];
        }
        Check(stayed == 200 && moved == 100,
            "every object that a worker pinned stays, and every other object moves");
        Check(engine.GetPinnedRootCount() == 200 && engine.CountPausePinnedBlocks() == 0,
            "two workers added each pin, the list holds it once, and no bit is left");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "S7 concurrent mutator detaches");
    }

    // Objects of the pinned object heap lie in pinned blocks only, and no pause moves them.
    void ValidateS7PinnedBlocks(uint8_t bits)
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        LxrRcConfiguration configuration{
            16 * ImmixBlockGeometry::BlockBytes, 2 * LxrLargeObjectSpace::MinimumObjectBytes, 4, 4096, 4, bits,
        };
        configuration.evacuateNursery = true;
        configuration.evacuateMature = true;
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        LxrRcObjectModel model{};
        model.context = &roots;
        model.finishRelocation = RelocateMoving;
        if (!Check(engine.Initialize(configuration, adapter, &model) == LxrRcResult::Success &&
                engine.AttachMutator(&identity, &mutator) == LxrRcResult::Success,
                "S7 pinned-block engine initializes"))
        {
            return;
        }
        Type body(1024, 0);
        std::vector<Object*> pinnedObjects;
        std::vector<Object*> normal;
        for (size_t index = 0; index < 4; index++)
        {
            pinnedObjects.push_back(AllocatePinnedObject(engine, mutator, body));
            normal.push_back(Allocate(engine, mutator, body));
        }
        for (Object* object : pinnedObjects)
        {
            if (object == nullptr)
            {
                return;
            }
        }
        size_t pinnedInPinnedBlock = 0;
        size_t normalInNormalBlock = 0;
        size_t shared = 0;
        for (size_t index = 0; index < 4; index++)
        {
            pinnedInPinnedBlock += engine.IsPinnedBlock(StartOf(pinnedObjects[index]));
            normalInNormalBlock += !engine.IsPinnedBlock(StartOf(normal[index]));
            shared += BlockOfObject(engine, pinnedObjects[index]) == BlockOfObject(engine, normal[index]);
        }
        Check(pinnedInPinnedBlock == 4 && normalInNormalBlock == 4 && shared == 0,
            "pinned-object-heap objects lie in pinned blocks only, and no other object lies in a pinned block");
        size_t pinnedBlock = BlockOfObject(engine, pinnedObjects[0]);
        roots.objects = pinnedObjects;
        roots.objects.insert(roots.objects.end(), normal.begin(), normal.end());
        Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
            "a young pause completes");
        size_t stayed = 0;
        size_t moved = 0;
        for (size_t index = 0; index < 4; index++)
        {
            stayed += roots.objects[index] == pinnedObjects[index];
            moved += roots.objects[4 + index] != normal[index];
        }
        Check(stayed == 4 && moved == 4 && engine.GetNurseryMetrics().inPlacePinnedBytes == 4 * 1024,
            "the young pause copies no pinned-object-heap object, and it copies the other young objects");
        Check(engine.CollectFull(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success,
            "a full pause with mature evacuation completes");
        stayed = 0;
        for (size_t index = 0; index < 4; index++)
        {
            stayed += roots.objects[index] == pinnedObjects[index];
        }
        Check(stayed == 4 && engine.IsPinnedBlock(StartOf(pinnedObjects[0])),
            "the full pause does not evacuate a pinned block, although it is almost empty");
        // The objects die. The pinned block is released like any block.
        roots.objects.clear();
        Check(Epoch(engine, roots.objects) && Epoch(engine, roots.objects) && Epoch(engine, roots.objects),
            "the pauses after the last use complete");
        Check(BlockStateOf(engine, pinnedBlock) == ImmixBlockState::Unallocated,
            "a pinned block with no live object is released");
        // A block that was pinned and is free again goes to ordinary allocation, and then it is an ordinary block.
        Type page(20000, 0);
        bool reused = false;
        for (size_t index = 0; index < 64 && !reused; index++)
        {
            Object* object = Allocate(engine, mutator, page);
            if (object == nullptr)
            {
                break;
            }
            if (BlockOfObject(engine, object) == pinnedBlock)
            {
                reused = true;
                Check(!engine.IsPinnedBlock(StartOf(object)),
                    "a released pinned block is an ordinary block when an ordinary allocation takes it");
            }
        }
        Check(reused, "the released pinned block is taken again by ordinary allocation");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "S7 pinned-block mutator detaches");
    }

    // The holes of a pinned block go to pinned allocation only.
    void ValidateS7PinnedRecycling()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        int identity;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Type body(2048, 0);
        Object* first = AllocatePinnedObject(engine, mutator, body);
        Object* second = AllocatePinnedObject(engine, mutator, body);
        Object* third = AllocatePinnedObject(engine, mutator, body);
        if (first == nullptr || second == nullptr || third == nullptr)
        {
            return;
        }
        size_t block = BlockOfObject(engine, first);
        std::vector<Object*> roots{first, third};
        Check(BlockOfObject(engine, second) == block && BlockOfObject(engine, third) == block,
            "three pinned objects share one pinned block");
        Check(Epoch(engine, roots), "the pause promotes the pinned block in place");
        uint8_t lines = 0;
        Check(BlockStateOf(engine, block, &lines) == ImmixBlockState::Reusable && lines < 64 &&
                engine.IsPinnedBlock(StartOf(first)),
            "the lazy sweep makes the pinned block Reusable and it stays pinned");
        Type little(24, 0);
        size_t inPinnedBlock = 0;
        for (size_t index = 0; index < 3000; index++)
        {
            Object* object = Allocate(engine, mutator, little);
            if (object == nullptr)
            {
                break;
            }
            inPinnedBlock += BlockOfObject(engine, object) == block;
        }
        Check(inPinnedBlock == 0, "ordinary allocation never takes the holes of a pinned block");
        Object* again = AllocatePinnedObject(engine, mutator, body);
        Check(again != nullptr && BlockOfObject(engine, again) == block,
            "pinned allocation reuses the holes of a pinned block");
        Check(engine.DetachMutator(mutator) == LxrRcResult::Success, "S7 recycling mutator detaches");
    }

    void ValidateS7Pins()
    {
        ValidateS7PinListUnit();
        ValidateS7PinListConcurrent();
        for (uint8_t bits : {uint8_t{2}, uint8_t{4}, uint8_t{8}})
        {
            ValidateS7NurseryPin(bits);
            ValidateS7InteriorPin(bits);
            ValidateS7ConcurrentPins(bits);
            ValidateS7PinnedBlocks(bits);
        }
        ValidateS7PinnedRecycling();
    }

    // ---- Root capture fix. A fresh block that one thread has taken and returned in the current epoch holds live
    // objects until the pause. A second thread that read the block state before the first thread took the block must
    // not take it afterwards: the clear of a fresh block would erase the objects of the first thread. ----

    struct FreshRaceContext final
    {
        ImmixBlockManager* blocks;
        uintptr_t owner;
        bool armed;
        bool taken;
        bool returned;
        uintptr_t block;
    };

    constexpr uint64_t FreshRaceSignature = UINT64_C(0x5ca1ab1e0badf00d);

    // Runs between the unlocked checks and the locked acquire of the allocating thread: another thread takes the same
    // block, writes its objects and returns the block.
    void FreshRaceHook(void* context, uintptr_t candidate)
    {
        FreshRaceContext* race = static_cast<FreshRaceContext*>(context);
        if (!race->armed)
        {
            return;
        }
        race->armed = false;
        ImmixBlockOperationStatus status;
        race->taken = race->blocks->TryAcquire(
                candidate, race->owner, ImmixBlockAcquireKind::MutatorFresh, &status) == SideMetadataResult::Success &&
            status == ImmixBlockOperationStatus::Updated;
        if (!race->taken)
        {
            return;
        }
        race->block = candidate;
        uint64_t* words = reinterpret_cast<uint64_t*>(candidate);
        for (size_t index = 0; index < ImmixBlockGeometry::BlockBytes / sizeof(uint64_t); index++)
        {
            words[index] = FreshRaceSignature + index;
        }
        race->returned = race->blocks->TryReturn(candidate, race->owner, &status) == SideMetadataResult::Success &&
            status == ImmixBlockOperationStatus::Updated;
    }

    void ValidateFreshBlockRace()
    {
        constexpr size_t BlockCount = 4;
        constexpr size_t ArenaSize = BlockCount * ImmixBlockGeometry::BlockBytes;
        constexpr uintptr_t OwnerFirst = 0x2400;
        constexpr uintptr_t OwnerSecond = 0x2500;
        constexpr uintptr_t OwnerThird = 0x2600;
        uint8_t* arena = static_cast<uint8_t*>(GCToOSInterface::VirtualReserve(
            ArenaSize, ImmixBlockGeometry::BlockBytes, VirtualReserveFlags::None));
        if (!Check(arena != nullptr && reinterpret_cast<uintptr_t>(arena) < ImmixBlockGeometry::AddressLimit &&
                GCToOSInterface::VirtualCommit(arena, ArenaSize), "the fresh block race arena is reserved"))
        {
            return;
        }
        uintptr_t start = reinterpret_cast<uintptr_t>(arena);
        LxrSideMetadataLayout layout;
        SideMetadataManager metadata;
        ImmixBlockManager blocks;
        ImmixAllocationManager allocation;
        ImmixBumpAllocator allocator;
        bool ready = LxrSideMetadataLayout::Create(1, &layout) == SideMetadataResult::Success &&
            metadata.Initialize(&layout) == SideMetadataResult::Success &&
            blocks.Initialize(&metadata) == SideMetadataResult::Success &&
            blocks.RegisterBlockRange(start, ArenaSize) == SideMetadataResult::Success &&
            allocation.Initialize(&metadata, &blocks, start, ArenaSize, 1) == SideMetadataResult::Success &&
            allocator.Initialize(&allocation, OwnerSecond) == SideMetadataResult::Success;
        if (Check(ready, "the fresh block race fixture initializes"))
        {
            FreshRaceContext race{&blocks, OwnerFirst, true, false, false, 0};
            allocation.SetAfterPrecheckHookForTest(FreshRaceHook, &race);
            ImmixAllocationStatus status = ImmixAllocationStatus::InvalidRequest;
            SideMetadataResult refill = allocator.TryRefill(
                ImmixAllocationSource::MutatorFresh, ImmixMarkingContext::Inactive, &status);
            allocation.SetAfterPrecheckHookForTest(nullptr, nullptr);
            Check(race.taken && race.returned,
                "the first thread takes the candidate block, fills it and returns it inside the window");
            Check(refill == SideMetadataResult::Success && status == ImmixAllocationStatus::Ready &&
                    allocator.GetBlock() != 0,
                "the second thread still gets a block");
            Check(allocator.GetBlock() != race.block,
                "the second thread does not take the block that the first thread filled in the same epoch");
            const uint64_t* words = reinterpret_cast<const uint64_t*>(race.block);
            size_t damaged = 0;
            for (size_t index = 0; index < ImmixBlockGeometry::BlockBytes / sizeof(uint64_t); index++)
            {
                damaged += words[index] != FreshRaceSignature + index ? 1 : 0;
            }
            Check(damaged == 0, "the objects of the first thread are not cleared by the second thread");

            ImmixBlockOperationStatus blockStatus;
            bool nursery = false;
            Check(blocks.IsNursery(race.block, &nursery) == SideMetadataResult::Success && nursery,
                "the returned block is still a nursery block of this epoch");
            SideMetadataResult direct = blocks.TryAcquire(
                race.block, OwnerThird, ImmixBlockAcquireKind::MutatorFresh, &blockStatus);
            Check(direct == SideMetadataResult::Success && blockStatus == ImmixBlockOperationStatus::Stale,
                "a locked fresh mutator acquire of a nursery block is stale");

            // The next epoch: the block is no longer a nursery block, and a fresh mutator acquire may take it.
            ImmixBlockOperationStatus pause;
            bool flipped = blocks.StartGcPause(&pause) == SideMetadataResult::Success &&
                pause == ImmixBlockOperationStatus::Updated &&
                blocks.ReleaseGcPause(&pause) == SideMetadataResult::Success &&
                pause == ImmixBlockOperationStatus::Updated;
            Check(flipped, "the pause flips the epoch");
            direct = blocks.TryAcquire(race.block, OwnerThird, ImmixBlockAcquireKind::MutatorFresh, &blockStatus);
            Check(direct == SideMetadataResult::Success && blockStatus == ImmixBlockOperationStatus::Updated,
                "after the epoch flip a fresh mutator acquire takes the block");
        }
        allocation.Shutdown();
        blocks.Shutdown();
        metadata.Shutdown();
        GCToOSInterface::VirtualRelease(arena, ArenaSize);
    }

    // ---- P22 (D2): the stock bump-pointer helpers. The engine hands out a buffer after it has prepared the whole
    // range, and learns about the objects of the buffer only as spans when the buffer is retired. ----

    // The test plays the stock allocation helper and the slow path of GCHeap::Alloc: it bumps inside the published
    // buffer, and calls the engine when the buffer cannot hold the object.
    struct Helper
    {
        LxrReferenceCounting* engine = nullptr;
        LxrRcMutator* mutator = nullptr;
        uintptr_t cursor = 0;
        uintptr_t limit = 0;
        size_t slowCalls = 0;

        Object* New(MethodTable* table, size_t size, uint32_t length = UINT32_MAX)
        {
            Object* object = nullptr;
            if (limit - cursor >= size)
            {
                object = reinterpret_cast<Object*>(cursor + sizeof(ObjHeader));
                cursor += size;
                engine->RecordHelperObjectForTest(object, size);
            }
            else
            {
                Retire();
                if (engine->Allocate(mutator, size, &object) != LxrRcResult::Success ||
                    engine->GetAllocationSpan(mutator, &cursor, &limit) != LxrRcResult::Success)
                {
                    return nullptr;
                }
                slowCalls++;
            }
            object->RawSetMethodTable(table);
            if (length != UINT32_MAX)
            {
                *reinterpret_cast<uint32_t*>(
                    reinterpret_cast<uint8_t*>(object) + ArrayBase::GetOffsetOfNumComponents()) = length;
            }
            return object;
        }

        Object* NewObject(Type& type) { return New(type.table, type.table->GetBaseSize()); }

        // What the pause does for every thread (GcEnumAllocContexts): the buffer ends at the used point.
        void Retire()
        {
            if (limit != 0)
            {
                engine->RetireAllocationSpan(mutator, cursor);
                cursor = 0;
                limit = 0;
            }
        }
    };

    // An array-like type with any component size (a string has 2).
    class ComponentType final
    {
    public:
        explicit ComponentType(uint16_t componentSize, uint32_t baseSize = 24)
            : m_bytes(sizeof(MethodTable), 0), table(reinterpret_cast<MethodTable*>(m_bytes.data()))
        {
            table->m_baseSize = baseSize;
            table->m_flags = MTFlag_HasComponentSize | MTFlag_IsArray;
            table->m_componentSize = componentSize;
        }

    private:
        std::vector<uint8_t> m_bytes;

    public:
        MethodTable* table;
    };

    size_t SizeOf(Object* object)
    {
        return (object->GetSize() + sizeof(uintptr_t) - 1) & ~(sizeof(uintptr_t) - 1);
    }

    constexpr uintptr_t BlockOf(uintptr_t address) { return address & ~ImmixBlockGeometry::BlockMask; }

    // Refill: the whole range of a buffer is made clean before the first object is allocated, so no later step
    // clears anything per object. The planes of every block are poisoned first, so a plane that the refill does not
    // clear shows up. With a trace running, the range is also marked in bulk.
    void ValidateD2Refill()
    {
        for (bool marking : {false, true})
        {
            LxrReferenceCounting engine;
            LxrRcMutator* mutator;
            int identity;
            if (!Initialize(engine, 8, &mutator, &identity))
            {
                return;
            }
            std::vector<Object*> none;
            if (marking && !Check(engine.HoldTrace(true) == LxrRcResult::Success &&
                    TracePause(engine, none, LxrPauseKind::InitialMark) && engine.IsMarkingForTest(),
                    "a trace is running"))
            {
                return;
            }
            uintptr_t arena = engine.GetReservationStart();
            size_t blocks = engine.GetImmixCapacityBytes() / ImmixBlockGeometry::BlockBytes;
            Check(engine.PoisonRangeForTest(arena, blocks * ImmixBlockGeometry::BlockBytes, false) ==
                    LxrRcResult::Success,
                "the ValidObject, unlog and mark planes of every block are poisoned (the count and straddle planes are not: F9)");
            Type type(24, 0);
            Helper helper{&engine, mutator};
            Object* first = helper.NewObject(type);
            if (first == nullptr)
            {
                return;
            }
            uintptr_t block = BlockOf(StartOf(first));
            LxrReferenceCounting::RangeStateForTest state;
            Check(engine.GetRangeStateForTest(block, ImmixBlockGeometry::BlockBytes, &state) == LxrRcResult::Success &&
                state.validSlots == 0 && state.countedSlots == 0 && state.unloggedSlots == 0 &&
                state.straddleLines == 0,
                "the refill clears the ValidObject and unlog planes of the whole range, and the count and straddle planes stay zero");
            constexpr size_t SlotsPerBlock = ImmixBlockGeometry::BlockBytes / sizeof(uintptr_t);
            Check(state.markedSlots == (marking ? SlotsPerBlock : 0),
                marking ? "a refill during a trace marks the whole range" : "a refill outside a trace leaves no mark");
            uintptr_t other = block == arena ? arena + 5 * ImmixBlockGeometry::BlockBytes : arena;
            LxrReferenceCounting::RangeStateForTest untouched;
            engine.GetRangeStateForTest(other, ImmixBlockGeometry::BlockBytes, &untouched);
            Check(engine.GetRangeStateForTest(other, ImmixBlockGeometry::BlockBytes, &untouched) ==
                    LxrRcResult::Success && untouched.validSlots == SlotsPerBlock,
                "a block that no buffer took keeps its poison");
            // The helper allocates 100 objects with no call into the engine, and nothing changes in the planes.
            size_t calls = helper.slowCalls;
            for (size_t index = 0; index < 100; index++)
            {
                helper.NewObject(type);
            }
            LxrReferenceCounting::RangeStateForTest after;
            Check(helper.slowCalls == calls && engine.GetRangeStateForTest(
                    block, ImmixBlockGeometry::BlockBytes, &after) == LxrRcResult::Success &&
                    after.validSlots == 0 && after.countedSlots == 0 && after.markedSlots == state.markedSlots,
                "allocation in the buffer touches no plane");
            if (marking)
            {
                helper.Retire();
                Check(engine.HoldTrace(false) == LxrRcResult::Success, "the trace is released");
            }
        }
    }

    // A recycled hole is prepared like a fresh block, and its neighbours keep their state.
    void ValidateD2RecycledHole()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator;
        int identity;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Type tiny(24, 0);
        Type wide(512, 0);
        Helper helper{&engine, mutator};
        std::vector<Object*> keep;
        for (size_t index = 0; index < 120; index++)
        {
            Object* object = helper.New(index % 3 == 0 ? wide.table : tiny.table, index % 3 == 0 ? 512 : 24);
            if (object == nullptr)
            {
                return;
            }
            if (index % 3 == 0)
            {
                keep.push_back(object);
            }
        }
        helper.Retire();
        if (!Epoch(engine, keep))
        {
            return;
        }
        uintptr_t counted = 0;
        for (Object* object : keep)
        {
            uintptr_t count = 0;
            engine.LoadCount(object, &count);
            counted += count;
        }
        Check(counted == keep.size() && engine.IsObjectStart(keep[0]), "the survivors of the block are promoted");
        // The block is reusable now. A buffer in it comes from a hole between the survivors.
        helper = Helper{&engine, mutator};
        std::vector<Object*> fresh;
        for (size_t index = 0; index < 200; index++)
        {
            Object* object = helper.NewObject(tiny);
            if (object == nullptr)
            {
                return;
            }
            fresh.push_back(object);
        }
        helper.Retire();
        size_t inRecycled = 0;
        for (Object* object : fresh)
        {
            LxrReferenceCounting::RangeStateForTest state;
            engine.GetRangeStateForTest(StartOf(object), 24, &state);
            inRecycled += BlockOf(StartOf(object)) == BlockOf(StartOf(keep[0])) &&
                state.validSlots == 0 && state.countedSlots == 0 && state.unloggedSlots == 0;
        }
        Check(inRecycled != 0, "new objects take holes of the reusable block, and their range is clean");
        bool intact = true;
        for (Object* object : keep)
        {
            uintptr_t count = 0;
            engine.LoadCount(object, &count);
            intact = intact && count == 1 && engine.IsObjectStart(object);
        }
        Check(intact, "the refill of a hole leaves the counts and start bits of its neighbours alone");
    }

    struct SpanProbe
    {
        std::vector<std::pair<uintptr_t, uintptr_t>> spans;
        std::vector<uintptr_t> lookups;
        std::vector<Object*> expected;
        std::vector<Object*> found;
        std::vector<LxrRcResult> results;
        std::vector<Object*> roots;
    };

    LxrRcResult CaptureSpans(LxrReferenceCounting* engine, void* context)
    {
        SpanProbe* probe = static_cast<SpanProbe*>(context);
        probe->spans.clear();
        for (size_t index = 0; index < engine->GetYoungSpanCount(); index++)
        {
            uintptr_t start;
            uintptr_t end;
            engine->GetYoungSpan(index, &start, &end);
            probe->spans.emplace_back(start, end);
        }
        probe->found.assign(probe->lookups.size(), nullptr);
        probe->results.assign(probe->lookups.size(), LxrRcResult::Success);
        for (size_t index = 0; index < probe->lookups.size(); index++)
        {
            probe->results[index] = engine->FindContainingObject(probe->lookups[index], &probe->found[index]);
        }
        for (Object* object : probe->roots)
        {
            LxrRcResult result = engine->AddRoot(object);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        return LxrRcResult::Success;
    }

    // Spans are recorded per thread, merged and sorted at the pause, cut at the used point, and the memory
    // after the cut is not parsed. A thread that has left (FixAllocContext) keeps its spans until the pause.
    void ValidateD2Spans()
    {
        LxrReferenceCounting engine;
        LxrRcConfiguration configuration{
            16 * ImmixBlockGeometry::BlockBytes, 8 * LxrLargeObjectSpace::MinimumObjectBytes, 16, 1024, 4, 8,
        };
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        int identities[4];
        LxrRcMutator* mutators[4] = {};
        if (!Check(engine.Initialize(configuration, adapter) == LxrRcResult::Success, "span engine initializes"))
        {
            return;
        }
        for (int index = 0; index < 4; index++)
        {
            if (!Check(engine.AttachMutator(&identities[index], &mutators[index]) == LxrRcResult::Success,
                    "span mutator attaches"))
            {
                return;
            }
        }
        Type type(24, 0);
        Type big(256, 0);
        Helper helpers[4];
        std::vector<Object*> objects[4];
        for (int index = 0; index < 4; index++)
        {
            helpers[index] = Helper{&engine, mutators[index]};
            for (size_t count = 0; count < 20 + 7 * static_cast<size_t>(index); count++)
            {
                Object* object = count % 5 == 4 ? helpers[index].NewObject(big) : helpers[index].NewObject(type);
                if (object == nullptr)
                {
                    return;
                }
                objects[index].push_back(object);
            }
        }
        // Mutator 1 stops with a buffer that is only partly used. The tail holds a well-formed object that the
        // collector must not see, because it lies after the used point.
        Object* ghost = reinterpret_cast<Object*>(helpers[1].cursor + sizeof(ObjHeader));
        ghost->RawSetMethodTable(type.table);
        uintptr_t ghostStart = StartOf(ghost);
        uintptr_t usedEnd1 = helpers[1].cursor;
        // Mutator 3 leaves before the pause, as a thread does that exits.
        helpers[3].Retire();
        uintptr_t usedEnd3 = StartOf(objects[3].back()) + SizeOf(objects[3].back());
        Check(engine.DetachMutator(mutators[3]) == LxrRcResult::Success, "a thread detaches with an open span");
        // The runtime enumerates the allocation contexts of the other threads at the pause.
        for (int index = 0; index < 3; index++)
        {
            helpers[index].Retire();
        }
        SpanProbe probe;
        probe.lookups = {
            reinterpret_cast<uintptr_t>(objects[0].back()) + 8,
            reinterpret_cast<uintptr_t>(objects[1].back()) + 8,
            reinterpret_cast<uintptr_t>(objects[3].back()) + 8,
            ghostStart + 16,
        };
        probe.roots = {objects[0].back(), objects[1].back(), objects[2].back(), objects[3].back()};
        Check(engine.Collect(CaptureSpans, &probe, Timeout) == LxrRcResult::Success &&
            engine.Drain(Timeout) == LxrRcResult::Success, "the pause completes");
        Check(probe.spans.size() == 4, "one span per thread is merged, the detached thread included");
        bool sorted = std::is_sorted(probe.spans.begin(), probe.spans.end());
        Check(sorted, "the merged spans are sorted by address");
        auto spanOf = [&](Object* first) -> std::pair<uintptr_t, uintptr_t>
        {
            for (auto& span : probe.spans)
            {
                if (span.first == StartOf(first))
                {
                    return span;
                }
            }
            return {0, 0};
        };
        for (int index = 0; index < 4; index++)
        {
            uintptr_t used = StartOf(objects[index].back()) + SizeOf(objects[index].back());
            size_t bytes = 0;
            for (Object* object : objects[index])
            {
                bytes += SizeOf(object);
            }
            auto span = spanOf(objects[index].front());
            Check(span.second == used && span.second - span.first == bytes,
                "the span of a thread covers exactly the used part of its buffers");
        }
        Check(spanOf(objects[1].front()).second == usedEnd1, "the span of a partly used buffer ends at the cut");
        Check(spanOf(objects[3].front()).second == usedEnd3, "the span of a detached thread ends at its used point");
        Check(probe.results[0] == LxrRcResult::Success && probe.found[0] == objects[0].back(),
            "an interior address in the last object of a span is found");
        Check(probe.results[1] == LxrRcResult::Success && probe.found[1] == objects[1].back(),
            "an interior address in the last object before the cut is found");
        Check(probe.results[2] == LxrRcResult::Success && probe.found[2] == objects[3].back(),
            "an interior address of a detached thread's object is found");
        Check(probe.results[3] == LxrRcResult::InvalidArgument && probe.found[3] == nullptr,
            "an address after the cut is not an object, although the memory there parses");
        size_t totalBytes = 0;
        for (int index = 0; index < 4; index++)
        {
            for (Object* object : objects[index])
            {
                totalBytes += SizeOf(object);
            }
        }
        Check(engine.GetMetrics().allocatedBytes == totalBytes && engine.GetYoungSpanCount() == 0,
            "the used bytes of the spans are counted, and the spans are dropped when the pause ends");
    }

    // Interior addresses inside young objects of many sizes are found through the spans: random addresses, many
    // objects, objects that cross lines, arrays, strings, the last object of a buffer, and holes that are reused.
    void ValidateD2InteriorLookup()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator;
        int identity;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        ArrayType pointers;
        ComponentType strings(2, 22);
        ComponentType bytes(1, 24);
        Type tiny(24, 0);
        Type medium(96, 0);
        Type wide(1000, 0);
        std::mt19937_64 random(0x5eed);
        Helper helper{&engine, mutator};
        size_t lookups = 0;
        size_t wrong = 0;
        std::vector<Object*> survivors;
        for (size_t round = 0; round < 6; round++)
        {
            std::vector<Object*> objects;
            std::vector<size_t> sizes;
            for (size_t index = 0; index < 1500; index++)
            {
                Object* object = nullptr;
                switch (random() % 6)
                {
                    case 0: object = helper.NewObject(tiny); break;
                    case 1: object = helper.NewObject(medium); break;
                    case 2: object = helper.NewObject(wide); break;
                    case 3:
                    {
                        uint32_t length = static_cast<uint32_t>(random() % 200);
                        object = helper.New(pointers.table, 24 + length * 8u, length);
                        break;
                    }
                    case 4:
                    {
                        uint32_t length = static_cast<uint32_t>(random() % 300);
                        object = helper.New(strings.table, (22 + length * 2u + 7) & ~7u, length);
                        break;
                    }
                    default:
                    {
                        uint32_t length = static_cast<uint32_t>(random() % 500);
                        object = helper.New(bytes.table, (24 + length + 7) & ~7u, length);
                        break;
                    }
                }
                if (object == nullptr)
                {
                    return;
                }
                objects.push_back(object);
                sizes.push_back(SizeOf(object));
            }
            helper.Retire();
            SpanProbe probe;
            for (size_t index = 0; index < objects.size(); index++)
            {
                for (int sample = 0; sample < 3; sample++)
                {
                    size_t offset = sizeof(uintptr_t) + random() % (sizes[index] - sizeof(ObjHeader) - 1);
                    probe.lookups.push_back(reinterpret_cast<uintptr_t>(objects[index]) + offset);
                    probe.expected.push_back(objects[index]);
                }
            }
            // The last object of every span, at its last byte.
            for (size_t index = 0; index < objects.size(); index++)
            {
                uintptr_t end = StartOf(objects[index]) + sizes[index];
                if (index + 1 == objects.size() || StartOf(objects[index + 1]) != end)
                {
                    probe.lookups.push_back(end - 1);
                    probe.expected.push_back(objects[index]);
                }
            }
            // A survivor of every round: some are kept, so that the next round reuses holes between them.
            probe.roots.clear();
            for (size_t index = 0; index < objects.size(); index += 17)
            {
                probe.roots.push_back(objects[index]);
            }
            if (!Check(engine.Collect(CaptureSpans, &probe, Timeout) == LxrRcResult::Success &&
                    engine.Drain(Timeout) == LxrRcResult::Success, "the interior lookup pause completes"))
            {
                return;
            }
            for (size_t index = 0; index < probe.lookups.size(); index++)
            {
                lookups++;
                wrong += probe.results[index] != LxrRcResult::Success || probe.found[index] != probe.expected[index];
            }
            survivors.insert(survivors.end(), probe.roots.begin(), probe.roots.end());
            helper = Helper{&engine, mutator};
        }
        Check(lookups > 20000 && wrong == 0, "every random interior address finds the object that contains it");
        // The holes between the survivors were reused in the rounds after the first.
        std::set<uintptr_t> blocks;
        for (Object* object : survivors)
        {
            blocks.insert(BlockOf(StartOf(object)));
        }
        Check(blocks.size() < survivors.size() / 8, "survivors of different rounds share blocks, so holes were reused");
    }

    // Objects that are allocated while a trace runs are marked in bulk, and no pause sees them one by one. They
    // survive the cycle sweep even when the pause counts them without tracing them.
    struct FinalRoots
    {
        std::vector<Object*> traced;
        std::vector<Object*> untraced;
    };

    LxrRcResult CaptureFinal(LxrReferenceCounting* engine, void* context)
    {
        FinalRoots* roots = static_cast<FinalRoots*>(context);
        for (Object* object : roots->traced)
        {
            LxrRcResult result = engine->AddRoot(object, false, true);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        for (Object* object : roots->untraced)
        {
            LxrRcResult result = engine->AddRoot(object, false, false);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        return LxrRcResult::Success;
    }

    void ValidateD2ConcurrentMarking()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator;
        int identity;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Type holder(24, 1);
        Type leaf(24, 0);
        Helper helper{&engine, mutator};
        Object* old = helper.NewObject(holder);
        helper.Retire();
        if (old == nullptr || !Epoch(engine, {old}))
        {
            return;
        }
        std::vector<Object*> oldRoot{old};
        if (!Check(engine.HoldTrace(true) == LxrRcResult::Success &&
                TracePause(engine, oldRoot, LxrPauseKind::InitialMark) && engine.IsMarkingForTest(),
                "a concurrent trace starts"))
        {
            return;
        }
        helper = Helper{&engine, mutator};
        std::vector<Object*> born;
        for (size_t index = 0; index < 300; index++)
        {
            Object* object = helper.NewObject(index % 2 == 0 ? leaf : holder);
            if (object == nullptr)
            {
                return;
            }
            born.push_back(object);
        }
        helper.Retire();
        // born[0] is stored into an object that the trace has scanned already. Every third object is known to the
        // final pause only as a counted root that the trace does not follow. The others are dead.
        Check(Store(engine, mutator, Field(old), born[0]), "a newborn object is stored into a scanned mature object");
        FinalRoots final;
        final.traced = {old};
        for (size_t index = 1; index < born.size(); index += 3)
        {
            final.untraced.push_back(born[index]);
        }
        Check(engine.HoldTrace(false) == LxrRcResult::Success, "the trace may go on");
        Check(engine.FinishTrace(CaptureFinal, &final, Timeout) == LxrRcResult::Success &&
            engine.Drain(Timeout) == LxrRcResult::Success, "the trace completes with a final pause");
        auto count = [&](Object* object)
        {
            uintptr_t value = UINTPTR_MAX;
            engine.LoadCount(object, &value);
            return value;
        };
        size_t kept = 0;
        size_t marked = 0;
        for (Object* object : final.untraced)
        {
            kept += count(object) == 1;
            marked += engine.IsMarked(object);
        }
        Check(count(born[0]) == 1 && engine.IsMarked(born[0]),
            "an object that was stored into a scanned object survives the trace");
        Check(kept == final.untraced.size() && marked == final.untraced.size(),
            "objects that were allocated during the trace are marked and are not swept as dead cycles");
        Check(engine.GetTraceMetrics().cycleObjects == 0, "the cycle sweep reclaims nothing that is live");
        size_t dead = 0;
        for (size_t index = 2; index < born.size(); index += 3)
        {
            dead += count(born[index]) == 0;
        }
        Check(dead == (born.size() - 2 + 2) / 3, "the objects that nothing referred to are dead");
    }

    // A pinned-object-heap object and a large object are allocated one by one, so they get their ValidObject bit
    // at the allocation and need no span. A small object of the ordinary heap does not.
    void ValidateD2SlowCategories()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator;
        int identity;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return;
        }
        Type pinnedType(96, 1);
        Type largeType(static_cast<uint32_t>(LxrLargeObjectSpace::MinimumObjectBytes + 4096), 0);
        Type tiny(24, 0);
        Object* pinned = AllocatePinnedObject(engine, mutator, pinnedType);
        Object* large = Allocate(engine, mutator, largeType);
        Object* young = Allocate(engine, mutator, tiny);
        if (pinned == nullptr || large == nullptr || young == nullptr)
        {
            return;
        }
        Check(engine.IsObjectStart(pinned) && engine.IsPinnedBlock(StartOf(pinned)),
            "a small pinned-object-heap object has its bit at once, in a pinned block");
        Check(engine.IsObjectStart(large) && engine.IsLargeObject(large),
            "a large object has its bit at once");
        Check(!engine.IsObjectStart(young) && engine.IsOwnedObject(young),
            "a small object of the ordinary heap has no bit, and is still an owned object");
        Object* found = nullptr;
        Check(engine.FindContainingObject(reinterpret_cast<uintptr_t>(pinned) + 40, &found) == LxrRcResult::Success &&
                found == pinned,
            "an interior address in a pinned-object-heap object is found outside a pause");
        found = nullptr;
        Check(engine.FindContainingObject(reinterpret_cast<uintptr_t>(large) + 3000, &found) == LxrRcResult::Success &&
                found == large,
            "an interior address in a large object is found outside a pause");
        found = nullptr;
        Check(engine.FindContainingObject(reinterpret_cast<uintptr_t>(young) + 8, &found) ==
                LxrRcResult::InvalidArgument,
            "an interior address in a young object is not found outside a pause");
    }

    // Interior roots into young objects that a pause copies: the source objects have lost their first field, and
    // the walk over the span takes their sizes from the copies.
    void ValidateD2MovingInterior()
    {
        LxrReferenceCounting engine;
        LxrRcMutator* mutator = nullptr;
        MovingRoots roots;
        int identity;
        if (!InitializeMoving(engine, &roots, 16, &mutator, &identity, 8))
        {
            return;
        }
        ArrayType pointers;
        std::mt19937_64 random(77);
        Helper helper{&engine, mutator};
        std::vector<Object*> objects;
        for (uint32_t index = 0; index < 300; index++)
        {
            uint32_t length = 1 + static_cast<uint32_t>(random() % 60);
            Object* object = helper.New(pointers.table, 24 + length * 8u, length);
            if (object == nullptr)
            {
                return;
            }
            uintptr_t* payload = reinterpret_cast<uintptr_t*>(reinterpret_cast<uint8_t*>(object) + 16);
            for (uint32_t element = 0; element < length; element++)
            {
                payload[element] = index * 1000u + element;
            }
            objects.push_back(object);
            if (index % 4 == 0)
            {
                roots.objects.push_back(object);
            }
        }
        helper.Retire();
        Object* last = objects.back();
        uint32_t lastLength = ArrayLength(last);
        size_t offset = 16 + (lastLength / 2) * sizeof(uintptr_t);
        roots.interior = reinterpret_cast<Object*>(reinterpret_cast<uint8_t*>(last) + offset);
        Check(engine.Collect(CaptureMoving, &roots, Timeout) == LxrRcResult::Success &&
            engine.Drain(Timeout) == LxrRcResult::Success, "the copying pause with interior roots completes");
        Object* copy = reinterpret_cast<Object*>(reinterpret_cast<uint8_t*>(roots.interior) - offset);
        Check(copy != last && engine.IsObjectStart(copy) && ArrayLength(copy) == lastLength &&
                reinterpret_cast<uintptr_t*>(reinterpret_cast<uint8_t*>(copy) + 16)[lastLength / 2] ==
                    299u * 1000u + lastLength / 2,
            "an interior root into a young object behind many copied sources follows the copy");
        bool intact = true;
        for (size_t index = 0; index < roots.objects.size(); index++)
        {
            Object* object = roots.objects[index];
            uint32_t length = ArrayLength(object);
            intact = intact && engine.IsObjectStart(object) && length > 0 &&
                reinterpret_cast<uintptr_t*>(reinterpret_cast<uint8_t*>(object) + 16)[length - 1] ==
                    index * 4u * 1000u + length - 1;
        }
        Check(intact, "the copies keep their payload");
        Check(engine.GetNurseryMetrics().copiedObjects == roots.objects.size() + 1,
            "the young objects behind the walk and the interior target are copied");
    }

    struct StressContext
    {
        std::vector<Object*> exact;
        std::vector<uintptr_t> interior;
        std::vector<Object*> interiorExpected;
        size_t bad = 0;
        size_t lookups = 0;
    };

    LxrRcResult CaptureStress(LxrReferenceCounting* engine, void* context)
    {
        StressContext* stress = static_cast<StressContext*>(context);
        for (Object* object : stress->exact)
        {
            LxrRcResult result = engine->AddRoot(object);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        for (size_t index = 0; index < stress->interior.size(); index++)
        {
            Object* found = nullptr;
            stress->lookups++;
            if (engine->FindContainingObject(stress->interior[index], &found) != LxrRcResult::Success ||
                found != stress->interiorExpected[index])
            {
                stress->bad++;
                continue;
            }
            LxrRcResult result = engine->AddRoot(reinterpret_cast<Object*>(stress->interior[index]), true);
            if (result != LxrRcResult::Success)
            {
                return result;
            }
        }
        return LxrRcResult::Success;
    }

    // Threads allocate in a tight loop through the helper protocol. The controller stops them between two
    // allocations, retires their buffers as the runtime does, and runs a pause whose roots are the newest objects of
    // each thread: exact roots and interior addresses. Every one of them must survive with its payload.
    // (A thread cannot be suspended inside the helper in the runtime; the managed cells cover that with the real
    // suspension.)
    void ValidateD2Suspension()
    {
        constexpr int Threads = 4;
        LxrReferenceCounting engine;
        LxrRcConfiguration configuration{
            96 * ImmixBlockGeometry::BlockBytes, 8 * LxrLargeObjectSpace::MinimumObjectBytes, 16, 4096, 4, 8,
        };
        LxrPauseRuntimeAdapter adapter{Suspend, Resume, nullptr, false};
        if (!Check(engine.Initialize(configuration, adapter) == LxrRcResult::Success, "stress engine initializes"))
        {
            return;
        }
        int identities[Threads];
        LxrRcMutator* mutators[Threads] = {};
        for (int index = 0; index < Threads; index++)
        {
            if (!Check(engine.AttachMutator(&identities[index], &mutators[index]) == LxrRcResult::Success,
                    "stress mutator attaches"))
            {
                return;
            }
        }
        struct Newest
        {
            std::atomic<Object*> exact{nullptr};
            std::atomic<Object*> inner{nullptr};
            std::atomic<size_t> innerSize{0};
            std::atomic<uintptr_t> tag{0};
            std::atomic<uint64_t> exactGeneration{0};
            std::atomic<uint64_t> innerGeneration{0};
        };
        Newest newest[Threads];
        std::atomic<uint64_t> generation{1};
        Helper helpers[Threads];
        std::atomic<bool> stop{false};
        std::atomic<bool> pause{false};
        std::atomic<int> parked{0};
        ArrayType pointers;
        Type tiny(24, 0);
        Type medium(120, 0);
        std::vector<std::thread> workers;
        for (int index = 0; index < Threads; index++)
        {
            helpers[index] = Helper{&engine, mutators[index]};
            workers.emplace_back([&, index]()
            {
                std::mt19937_64 random(1000 + index);
                uintptr_t tag = 1;
                while (!stop.load())
                {
                    if (pause.load())
                    {
                        parked.fetch_add(1);
                        while (pause.load())
                        {
                            std::this_thread::yield();
                        }
                        parked.fetch_sub(1);
                        continue;
                    }
                    Object* object;
                    size_t size;
                    switch (random() % 3)
                    {
                        case 0: size = 24; object = helpers[index].New(tiny.table, size); break;
                        case 1: size = 120; object = helpers[index].New(medium.table, size); break;
                        default:
                        {
                            uint32_t length = static_cast<uint32_t>(random() % 40);
                            size = 24 + length * 8u;
                            object = helpers[index].New(pointers.table, size, length);
                            break;
                        }
                    }
                    if (object == nullptr)
                    {
                        std::this_thread::yield();
                        continue;
                    }
                    tag++;
                    if (size >= 32)
                    {
                        *reinterpret_cast<uintptr_t*>(reinterpret_cast<uint8_t*>(object) + 16) = tag * 7919 + index;
                    }
                    Object* previous = newest[index].exact.load();
                    newest[index].inner.store(previous);
                    newest[index].innerSize.store(previous == nullptr ? 0 : SizeOf(previous));
                    newest[index].innerGeneration.store(newest[index].exactGeneration.load());
                    newest[index].exact.store(object);
                    newest[index].exactGeneration.store(generation.load());
                    newest[index].tag.store(tag);
                }
            });
        }
        std::mt19937_64 controller(5);
        size_t failures = 0;
        size_t interiorChecks = 0;
        size_t pauses = 0;
        for (int round = 0; round < 300; round++)
        {
            std::this_thread::sleep_for(std::chrono::microseconds(controller() % 1500));
            pause.store(true);
            while (parked.load() != Threads)
            {
                std::this_thread::yield();
            }
            StressContext stress;
            std::vector<Object*> rooted;
            for (int index = 0; index < Threads; index++)
            {
                helpers[index].Retire();
                Object* exact = newest[index].exact.load();
                Object* inner = newest[index].inner.load();
                size_t innerSize = newest[index].innerSize.load();
                if (exact != nullptr)
                {
                    stress.exact.push_back(exact);
                    rooted.push_back(exact);
                }
                // The inner object is looked up only when the thread allocated it in this window: an older one is
                // dead or promoted, and its memory may be reused.
                if (inner != nullptr && newest[index].innerGeneration.load() == generation.load() && innerSize > 24)
                {
                    stress.interior.push_back(reinterpret_cast<uintptr_t>(inner) + innerSize / 2 - 8);
                    stress.interiorExpected.push_back(inner);
                    rooted.push_back(inner);
                }
            }
            bool collected = engine.Collect(CaptureStress, &stress, Timeout) == LxrRcResult::Success &&
                engine.Drain(Timeout) == LxrRcResult::Success;
            Check(collected, "a pause with threads stopped between allocations completes");
            pauses++;
            failures += stress.bad;
            interiorChecks += stress.lookups;
            for (Object* object : rooted)
            {
                uintptr_t count = 0;
                engine.LoadCount(object, &count);
                if (count == 0 || !engine.IsObjectStart(object) || object->GetGCSafeMethodTable() == nullptr)
                {
                    failures++;
                }
            }
            pause.store(false);
            generation.fetch_add(1);
            if (!collected)
            {
                break;
            }
        }
        stop.store(true);
        for (std::thread& worker : workers)
        {
            worker.join();
        }
        Check(pauses == 300 && failures == 0 && interiorChecks > 300,
            "every object that a root named in a pause survives, and every interior address finds its object");
    }
}

int main(int argc, char** argv)
{
    if (argc == 2 && strcmp(argv[1], "--d2-spans") == 0)
    {
        ValidateFastSpan();
        ValidateD2Refill();
        ValidateD2RecycledHole();
        ValidateD2Spans();
        ValidateD2InteriorLookup();
        ValidateD2ConcurrentMarking();
        ValidateD2SlowCategories();
        ValidateD2MovingInterior();
        ValidateD2Suspension();
        std::printf("%s: %d/%d D2 bump-allocation checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--fresh-block-race") == 0)
    {
        ValidateFreshBlockRace();
        std::printf("%s: %d/%d fresh block race checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--s7-pins") == 0)
    {
        ValidateS7Pins();
        std::printf("%s: %d/%d S7 pin checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--s6-registry") == 0)
    {
        ValidateS6Registry();
        std::printf("%s: %d/%d S6 registry checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--s5b-overshoot") == 0)
    {
        ValidateS5bOvershoot();
        std::printf("%s: %d/%d S5b overshoot checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--s4-sweep") == 0)
    {
        ValidateS4Sweep();
        std::printf("%s: %d/%d S4 sweep checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--s3-bookkeeping") == 0)
    {
        ValidateS3Bookkeeping();
        std::printf("%s: %d/%d S3 bookkeeping checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--lazy-large-release") == 0)
    {
        ValidateLazyLargeReleaseDuringTrace();
        std::printf("%s: %d/%d lazy large-object release checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--inline-metadata") == 0)
    {
        ValidateInlineMetadataAccessors();
        std::printf("%s: %d/%d inline metadata accessor checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--log-storage") == 0)
    {
        ValidateLogStorage();
        std::printf("%s: %d/%d log storage checks\n", Failures == 0 ? "PASS" : "FAIL",
            Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--mature") == 0)
    {
        for (uint8_t bits : {uint8_t{2}, uint8_t{4}, uint8_t{8}})
        {
            ValidateMatureMovement(bits, false);
            ValidateMatureMovement(bits, true);
        }
        ValidateMatureCopyFailure();
        std::printf("%s: %d/%d mature movement checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--mature-remset") == 0)
    {
        ValidateMatureRememberedReuse();
        std::printf("%s: %d/%d mature remset checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--mature-reader") == 0)
    {
        ValidateMatureMovingReader();
        std::printf("%s: %d/%d mature reader checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--mature-retained-owner") == 0)
    {
        ValidateRetainedMatureOwner(true);
        ValidateRetainedMatureOwner(false);
        std::printf("%s: %d/%d retained mature owner checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--mature-released-candidate") == 0)
    {
        ValidateMatureRememberedReuse(false, 1, false, 0, true);
        std::printf("%s: %d/%d released candidate incarnation checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--mature-headroom") == 0)
    {
        ValidateNurseryHeadroom();
        std::printf("%s: %d/%d nursery headroom checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--mature-completed-scan") == 0)
    {
        ValidateCompletedMatureScan();
        std::printf("%s: %d/%d completed mature descriptor checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && (strcmp(argv[1], "--mature-remset-whole") == 0 ||
            strcmp(argv[1], "--mature-remset-wrap") == 0))
    {
        bool wholeBlock = strcmp(argv[1], "--mature-remset-whole") == 0;
        ValidateMatureRememberedReuse(wholeBlock, wholeBlock ? 1 : 256);
        std::printf("%s: %d/%d %s remset lifetime checks\n", Failures == 0 ? "PASS" : "FAIL",
            Checks - Failures, Checks, wholeBlock ? "whole-block" : "byte-wrap");
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--mature-remset-same-shape") == 0)
    {
        ValidateMatureRememberedReuse(false, 255, true);
        std::printf("%s: %d/%d same-shape remset checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--mature-remset-boundaries") == 0)
    {
        for (uint8_t boundary : {uint8_t{1}, uint8_t{2}, uint8_t{3}, uint8_t{4}})
        {
            ValidateMatureRememberedReuse(false, 255, false, boundary);
        }
        std::printf("%s: %d/%d remset current-owner boundary checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--nursery") == 0)
    {
        for (uint8_t bits : {uint8_t{2}, uint8_t{4}, uint8_t{8}})
        {
            ValidateMovingNursery(bits);
        }
        std::printf("%s: %d/%d moving nursery checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--nursery-reader") == 0)
    {
        ValidateMovingReader();
        std::printf("%s: %d/%d moving reader checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--root-batching") == 0)
    {
        ValidateRootBatching();
        std::printf("%s: %d/%d root batching checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--trace-batching") == 0)
    {
        ValidateTraceBatching();
        ValidateTraceBatchSlices();
        std::printf("%s: %d/%d trace batching checks\n", Failures == 0 ? "PASS" : "FAIL",
            Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--aux-retirement") == 0)
    {
        ValidateAuxiliaryRetirement();
        std::printf("%s: %d/%d auxiliary retirement checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
#ifdef LXR_OBJECT_INTERIOR_CONTROL
    ValidateObjectPlanes();
    std::printf("%s: %d/%d object plane controls\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
    return Failures == 0 ? 0 : 1;
#endif // LXR_OBJECT_INTERIOR_CONTROL
    if (argc == 2 && strcmp(argv[1], "--object-planes") == 0)
    {
        ValidateObjectPlanes();
        std::printf("%s: %d/%d object plane checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
#ifdef LXR_STORAGE_SKIP_COPY_CONTROL
    ValidateStorageDiagnostics();
    std::printf("%s: %d/%d storage accounting controls\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
    return Failures == 0 ? 0 : 1;
#endif // LXR_STORAGE_SKIP_COPY_CONTROL
    if (argc == 2 && strcmp(argv[1], "--storage-diagnostics") == 0)
    {
        ValidateStorageDiagnostics();
        std::printf("%s: %d/%d storage accounting checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--birth-study") == 0)
    {
        return BirthStudy();
    }
#ifdef LXR_PROBE_SKIP_RECLAIM_CONTROL
    ValidateLifetimeProbe();
    std::printf("%s: %d/%d lifetime probe controls\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
    return Failures == 0 ? 0 : 1;
#endif // LXR_PROBE_SKIP_RECLAIM_CONTROL
    if (argc == 2 && strcmp(argv[1], "--lifetime") == 0)
    {
        ValidateLifetimeProbe();
        std::printf("%s: %d/%d lifetime probe checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
#if defined(LXR_POLICY_WRONG_UPDATE_CONTROL) || defined(LXR_POLICY_WRONG_ORDER_CONTROL)
    ValidatePolicy();
    ValidatePolicyIntegration();
    std::printf("%s: %d/%d policy controls\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
    return Failures == 0 ? 0 : 1;
#endif
    if (argc == 2 && strcmp(argv[1], "--p8-policy") == 0)
    {
        ValidatePolicy();
        ValidatePolicyIntegration();
        std::printf("%s: %d/%d policy checks\n", Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--p8-storage") == 0)
    {
        ValidateFastSpan();
        ValidateGrowth();
        ValidateGrowthFailure();
        ValidateCoalescedGrowth();
        ValidateMinimumGrowthBudget();
        std::printf("%s: %d/%d fast-span and growth checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--zero-rc-control") == 0)
    {
        return ZeroCountControl();
    }
    if (argc == 2 && strcmp(argv[1], "--benchmark") == 0)
    {
        return Benchmark();
    }
    if (argc == 2 && strcmp(argv[1], "--copy-study") == 0)
    {
        LxrReferenceCounting engine;
        int identity;
        LxrRcMutator* mutator;
        if (!Initialize(engine, 8, &mutator, &identity))
        {
            return 1;
        }
        std::printf("width,sample,copies,bytes,elapsed_ns\n");
        for (size_t width : {size_t{1}, size_t{64}, size_t{1024}})
        {
            for (size_t sample = 0; sample < 7; sample++)
            {
                size_t copies = 0;
                uint64_t nanoseconds = 0;
                if (!Check(engine.MeasureLogCopy(width, &copies, &nanoseconds) == LxrRcResult::Success &&
                        copies != 0, "actual flat-stream publication and copied payload agree"))
                {
                    return 1;
                }
                std::printf("%zu,%zu,%zu,%zu,%llu\n", width, sample, copies,
                    copies * width * sizeof(uintptr_t), static_cast<unsigned long long>(nanoseconds));
            }
        }
        return 0;
    }
    if (argc == 2 && strcmp(argv[1], "--first-store-churn") == 0)
    {
        ValidateFirstStoreBeforeAllocation();
        ValidateFirstStoreBeforeAllocation(true);
        std::printf("%s: %d/%d first-store engine checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--f1-trace") == 0)
    {
        for (bool nursery : {false, true})
        {
            ValidateF1BlackAllocation(nursery);
            ValidateF1RootsOnly(nursery);
        }
        ValidateBlackNewbornClosure();
        ValidateF1SnapshotRoot();
        for (uint32_t seed = 1; seed <= 8; seed++)
        {
            ValidateF1Stress(seed);
        }
        std::printf("%s: %d/%d F1 trace packet checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--f2-pause") == 0)
    {
        for (bool nursery : {false, true})
        {
            ValidateF2Backlog(nursery, 500);
            ValidateF2Backlog(nursery, 8000);
        }
        ValidateF2ShutdownWithBacklog();
        std::printf("%s: %d/%d F2 pause walk checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--f8-lazy") == 0)
    {
        for (uint32_t seed = 1; seed <= 6; seed++)
        {
            ValidateF8Stress(seed, false);
            ValidateF8Stress(seed, true);
        }
        std::printf("%s: %d/%d F8 lazy decrement checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--f4b-scheduler") == 0)
    {
        ValidateF4bTraceLoop();
        ValidateF4bTraceLimitDefault();
        ValidateF4bStealing();
        ValidateF4bStress();
        ValidateF4bPauseKeepsTracePackets();
        std::printf("%s: %d/%d F4b scheduler and trace loop checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--f4-scheduler") == 0)
    {
        ValidateF4WorkerPolicy();
        ValidateF4Wakeups();
        ValidateF4ConcurrentLimit();
        ValidateF4Recursion();
        std::printf("%s: %d/%d F4 scheduler and recursion checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--f10-rc") == 0)
    {
        for (int bits : {2, 4, 8})
        {
            ValidateF10Overflow(static_cast<uint8_t>(bits));
        }
        std::printf("%s: %d/%d F10 count width checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--f5-policy") == 0)
    {
        ValidateF5OccupiedPages(false);
        ValidateF5OccupiedPages(true);
        std::printf("%s: %d/%d F5 emergency accounting checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
    if (argc == 2 && strcmp(argv[1], "--trace") == 0)
    {
        constexpr uint8_t Widths[] = {2, 4, 8};
        for (uint8_t bits : Widths)
        {
            ValidateCycleReclamation(bits);
        }
        ValidateMarkAndScanOnDelete();
        ValidateSharedSatbStream();
        ValidateRetiredLeaseReuse();
        ValidateBlackNewbornClosure();
        std::printf("%s: %d/%d trace engine checks\n",
            Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
        return Failures == 0 ? 0 : 1;
    }
#if defined(LXR_RC_EARLY_ROOT_CONTROL) || defined(LXR_RC_SWEEP_QUERY_CONTROL)
    ValidateAcyclicAndRoots(8);
#else
    constexpr uint8_t Widths[] = {2, 4, 8};
    for (uint8_t bits : Widths)
    {
        ValidateAcyclicAndRoots(bits);
        ValidateNurseryAndReuse(bits);
        ValidateStickyAndCycles(bits);
        ValidateLargeObjects(bits);
        ValidateCycleReclamation(bits);
        ValidateF10Overflow(bits);
    }
    for (size_t fields : {1023, 1024, 1025, 4097})
    {
        ValidateOverflowAndExit(fields);
    }
    ValidateCompetingClaims();
    ValidateRecycledSizeFallback();
    ValidateChurnAdmission();
    ValidateAllocationDuringLazyDeath();
    ValidateFirstStoreBeforeAllocation();
    ValidateFirstStoreBeforeAllocation(true);
    ValidateMarkAndScanOnDelete();
    ValidateSharedSatbStream();
    ValidateRetiredLeaseReuse();
    ValidateBlackNewbornClosure();
#endif
    std::printf("%s: %d/%d integrated P4 checks\n",
        Failures == 0 ? "PASS" : "FAIL", Checks - Failures, Checks);
    return Failures == 0 ? 0 : 1;
}

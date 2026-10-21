// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "../../../../src/coreclr/gc/env/common.h"
#include "../../../../src/coreclr/gc/env/gcenv.h"
#include "../../../../src/coreclr/gc/gcref.h"
#include "../../../../src/coreclr/gc/lxr_reference_counting.h"
#include "../../../../src/coreclr/gc/lxr_collection_policy.h"

#include <atomic>
#include <algorithm>
#include <chrono>
#include <cstdio>
#include <thread>
#include <vector>

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
        return roots->interior == nullptr ? LxrRcResult::Success : engine->AddRoot(roots->interior, true);
    }

    bool MovingPinned(Object* object, void* context)
    {
        return object == static_cast<MovingRoots*>(context)->pinned;
    }

    LxrRcResult RelocateMoving(LxrReferenceCounting* engine, void* context)
    {
        MovingRoots* roots = static_cast<MovingRoots*>(context);
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
        model.isPinned = MovingPinned;
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
        model.isPinned = MovingPinned;
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
        model.isPinned = MovingPinned;
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
            Check(engine.GetStorageMetric(3) == 3 * count * sizeof(uintptr_t),
                "each root is copied once for RC, once for its increment trace and once for the batched trace seed");
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
                Check(engine.GetStorageMetric(53) >= count * 16 && engine.GetStorageMetric(54) != 0,
                    "increments and small-object descendants use the trace target accumulator");
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
                bool fast;
                if (engine.RecordFastAllocation(mutator, object, 24, &fast) != LxrRcResult::Success ||
                    !fast || engine.CompleteAllocation(object, 24, AllocationCompleteFlags::None, mutator) !=
                        LxrRcResult::Success)
                {
                    return 1;
                }
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
        bool fast = true;
        Check(engine.RecordFastAllocation(mutator, slow, 24, &fast) == LxrRcResult::Success && !fast,
            "refill object is not counted as a helper hit");
        Check(engine.CompleteAllocation(slow, 24, AllocationCompleteFlags::None) == LxrRcResult::Success,
            "refill birth is exact");
        std::vector<Object*> roots{slow};
        for (size_t index = 0; index < 512; index++)
        {
            Object* object = reinterpret_cast<Object*>(cursor + sizeof(ObjHeader));
            Check(engine.GetObjectExtent(object) == 0 && object->GetHeader()->GetBits() == 0,
                "unconsumed span has no registered object and a zero header");
            object->RawSetMethodTable(type.table);
            Check(engine.RecordFastAllocation(mutator, object, 24, &fast) == LxrRcResult::Success && fast,
                "helper consumption is contiguous, aligned and owned");
            Check(engine.CompleteAllocation(object, 24, AllocationCompleteFlags::None, mutator) == LxrRcResult::Success &&
                engine.GetObjectExtent(object) == 24, "every helper birth publishes its extent");
            cursor += 24;
            roots.push_back(object);
        }
        Check(engine.RetireAllocationSpan(mutator, cursor) == LxrRcResult::Success,
            "unused tail is reconciled before collection");
        Check(engine.GetMetrics().allocations == 513 && engine.GetMetrics().allocatedBytes == 513 * 24,
            "fast and slow accounting counts births, not whole spans");
        Epoch(engine, roots);
        Check(engine.GetLiveBytes() == 513 * 24, "collection retains all fast-born roots");
        Epoch(engine, {});
        Check(engine.GetLiveBytes() == 0, "fast-born objects and span holes reclaim normally");
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
                Check(engine.CompleteAllocation(object, bytes, AllocationCompleteFlags::None) == LxrRcResult::Success,
                    "newly admitted planes cover the complete birth");
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
        // Five object planes, four payload streams, fifteen metadata planes, then the managed heap.
        constexpr int64_t CommitCalls = 25;
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
            Check(engine.CompleteAllocation(object, 24, AllocationCompleteFlags::None) == LxrRcResult::Success,
                "preexisting birth completes before growth failure");
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
        Check(engine.CompleteAllocation(object, type.table->GetBaseSize(), AllocationCompleteFlags::None) ==
            LxrRcResult::Success, "coalesced extent has complete metadata");
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
        AllocationCompleteFlags flags = type.table->ContainsGCPointers()
            ? AllocationCompleteFlags::ContainsReferences : AllocationCompleteFlags::None;
        if (!Check(engine.CompleteAllocation(object, type.table->GetBaseSize(), flags) ==
                LxrRcResult::Success, "allocation completion registers exact physical start"))
        {
            return nullptr;
        }
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
        return Check(engine.CompleteAllocation(object, size, AllocationCompleteFlags::None) ==
                LxrRcResult::Success, "array allocation completes") ? object : nullptr;
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
        return Check(engine.CompleteAllocation(object, type.table->GetBaseSize(), AllocationCompleteFlags::None) ==
                LxrRcResult::Success, "random allocation completes") ? object : nullptr;
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
        Check(engine.IsObjectStart(dead) && engine.GetObjectExtent(dead) == 0 && Count(engine, dead) == 0,
            "a dead young object keeps a stale ValidObject bit while the oracle has reclaimed it");
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
    // pool in the pause. A block with a promoted object keeps its state, is not given to an allocator before the
    // lazy sweep has counted its lines, and is Reusable after that sweep.
    void ValidatePauseReleaseAndLazyReuse()
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
        // Allocation while the sweep is held must not use the unprocessed blocks.
        bool unprocessedUsed = false;
        for (size_t index = 0; index < 2800; index++)
        {
            Object* object = Allocate(engine, mutator, leaf);
            if (object == nullptr)
            {
                break;
            }
            size_t block = BlockOfObject(engine, object);
            unprocessedUsed = unprocessedUsed || block == blocks.first || block == blocks.last;
        }
        Check(!unprocessedUsed && BlockStateOf(engine, blocks.first) == ImmixBlockState::Unmarked &&
                BlockStateOf(engine, blocks.last) == ImmixBlockState::Unmarked,
            "no block is handed out before the lazy sweep has looked at it");
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
        ValidatePauseReleaseAndLazyReuse();
        ValidatePauseWaitsForLazySweep();
        for (int kind = 0; kind < 3; kind++)
        {
            ValidateBlockCounters(kind);
        }
    }
}

int main(int argc, char** argv)
{
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

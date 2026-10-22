// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// P17 S6: what a pause does with the finalizer registry. The collector calls these three functions from its scan
// callbacks; the native validation program calls the same functions, so the tests cover the code that runs in the
// product. See lxr_finalizer_registry.h for the states of an entry.

#ifndef LXR_FINALIZER_PAUSE_H
#define LXR_FINALIZER_PAUSE_H

#include "lxr_finalizer_registry.h"
#include "lxr_reference_counting.h"

struct LxrFinalizerPauseCounters
{
    std::atomic<uint64_t> retained{0};
    std::atomic<uint64_t> released{0};
    std::atomic<uint64_t> decisions{0};
    std::atomic<uint64_t> walked{0};
};

using LxrFinalizerPredicate = bool (*)(Object* object);

// Root capture of every pause. Gives the new entries their count, hands the counts that were given up to the engine,
// and (in a pause that traces) makes the objects that wait for their finalizer trace roots.
inline LxrRcResult LxrFinalizerCapture(
    LxrReferenceCounting& engine, LxrFinalizerRegistry& registry, LxrPauseKind kind,
    LxrFinalizerPredicate suppressed, LxrFinalizerPauseCounters* counters)
{
    LxrRcResult failure = LxrRcResult::Success;
    bool memory = registry.CaptureNew(suppressed, [&](Object* object)
    {
#if defined(LXR_S6_NO_RETAIN_CONTROL)
        LxrRcResult result = LxrRcResult::Success;
#else
        LxrRcResult result = engine.RetainRoot(object);
#endif // LXR_S6_NO_RETAIN_CONTROL
        if (result != LxrRcResult::Success)
        {
            failure = result;
        }
        if (counters != nullptr)
        {
            counters->retained.fetch_add(1, std::memory_order_relaxed);
        }
    }, [](Object* object) { return object->GetGCSafeMethodTable()->HasCriticalFinalizer(); });
    size_t released = registry.DrainReleased([&](Object* object)
    {
        LxrRcResult result = engine.ReleaseRoot(object);
        if (result != LxrRcResult::Success)
        {
            failure = result;
        }
    });
    if (counters != nullptr)
    {
        counters->released.fetch_add(released, std::memory_order_relaxed);
    }
#if defined(LXR_S6_RC_DECIDE_CONTROL)
    if (kind == LxrPauseKind::RefCount)
    {
        // Control: a reference-counting pause decides, with no marks, that every counted entry is unreachable.
        registry.Decide([&](Object* object) { return object; }, suppressed, [](Object*) { return false; },
            [](Object*) { return false; }, [](Object*) {});
    }
#endif // LXR_S6_RC_DECIDE_CONTROL
    if (kind != LxrPauseKind::RefCount)
    {
        // An object that waits for its finalizer must stay traced until the finalizer thread has taken it.
        registry.ForEachReady([&](Object* object, bool)
        {
            LxrRcResult result = engine.AddRoot(object, false, true);
            if (result != LxrRcResult::Success)
            {
                failure = result;
            }
        });
    }
    return memory ? failure : LxrRcResult::OutOfMemory;
}

// Tracing pause, after marking and before the weak closure ends. A reference-counting pause never calls it.
inline LxrRcResult LxrFinalizerDecide(
    LxrReferenceCounting& engine, LxrFinalizerRegistry& registry, LxrFinalizerPredicate suppressed,
    LxrFinalizerPredicate eager, LxrFinalizerPauseCounters* counters)
{
#if defined(LXR_S6_NO_DECIDE_CONTROL)
    return LxrRcResult::Success;
#endif // LXR_S6_NO_DECIDE_CONTROL
    LxrRcResult failure = LxrRcResult::Success;
    if (counters != nullptr)
    {
        counters->walked.fetch_add(registry.GetCountedEntries(), std::memory_order_relaxed);
    }
    // An unmarked object that is dropped (suppressed finalization or eager finalization) keeps a count until the next
    // pause takes it. The cycle sweep must not free it before that, so it is traced like a finalizable object.
    LxrWordArray traced;
    bool memory = registry.Decide(
        [&](Object* object) { return engine.ResolveForwarded(object); },
        suppressed,
        [&](Object* object) { return engine.IsMarked(object); },
        eager,
        [&](Object* object)
        {
            if (!traced.Push(reinterpret_cast<uintptr_t>(object)))
            {
                failure = LxrRcResult::OutOfMemory;
            }
        });
    if (!memory)
    {
        failure = LxrRcResult::OutOfMemory;
    }
    // Every unreachable candidate has been found before the first of them is traced, so that f-reachability cannot
    // mark another candidate.
    registry.ForEachNewReady([&](Object* object)
    {
        if (counters != nullptr)
        {
            counters->decisions.fetch_add(1, std::memory_order_relaxed);
        }
        if (!traced.Push(reinterpret_cast<uintptr_t>(object)))
        {
            failure = LxrRcResult::OutOfMemory;
        }
    });
#if !defined(LXR_S6_NO_TRACE_CONTROL)
    if (failure == LxrRcResult::Success)
    {
        failure = engine.TraceRoots(traced.Data(), traced.Size());
    }
#endif // !LXR_S6_NO_TRACE_CONTROL
    return failure;
}

// After the relocation of roots and handles. A nursery copy can move only the entries of the current batch. A mature
// copy happens in a tracing pause and can move any entry.
inline void LxrFinalizerRelocate(
    LxrReferenceCounting& engine, LxrFinalizerRegistry& registry, bool evacuateMature, LxrPauseKind kind)
{
#if defined(LXR_S6_NO_RELOCATE_CONTROL)
    return;
#endif // LXR_S6_NO_RELOCATE_CONTROL
    bool all = evacuateMature && kind != LxrPauseKind::RefCount;
    registry.Relocate(all, [&](Object* object) { return engine.ResolveForwarded(object); });
}

#endif // LXR_FINALIZER_PAUSE_H

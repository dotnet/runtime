// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"

#if defined(TARGET_BROWSER) && defined(PERFTRACING_DISABLE_THREADS)

#include <emscripten.h>
#include "method.hpp"
#include "typestring.h"
#include "wasm/browserprofiler.h"

extern "C" {
    void ds_rt_browser_performance_measure(void* pMethodDesc, double start);
}

static constexpr int MAX_STACK_DEPTH = 600;

struct ProfilerStackFrame
{
    MethodDesc *pMethod;
    double startMs;
    bool shouldRecord;
};

static ProfilerStackFrame s_profilerStack[MAX_STACK_DEPTH];
static int s_topStackFrameIndex = -1;

// Number of method enters that occurred while the shadow stack was already
// full. These frames are not recorded; the counter keeps subsequent leaves
// balanced so the profiler degrades gracefully instead of overflowing.
static int s_overflowDepth = 0;

// Adaptive recording state — controls how often we actually call
// performance.measure(). The shadow stack always tracks enter/leave for
// correctness, but recording is rate-limited so it occurs approximately
// once per s_desiredRecordIntervalMs. This mirrors the exponential moving
// average approach used by the EventPipe sampling profiler in
// ep-rt-coreclr-wasm-sampling.cpp (kept as a separate copy on purpose).
static double s_desiredRecordIntervalMs = 1.0;
static double s_lastRecordTimeMs = 0.0;
static int32_t s_prevSkipsPerPeriod = 1;
static int32_t s_skipsPerPeriod = 10;
static int32_t s_recordSkipCounter = 0;

// Recalculates s_skipsPerPeriod based on how long the last period actually
// took relative to the desired interval.
static void UpdateRecordFrequency()
{
    double now = emscripten_get_now();

    if (s_lastRecordTimeMs > 0.0)
    {
        double elapsed = now - s_lastRecordTimeMs;
        if (elapsed > 0.0)
        {
            double ratio = s_desiredRecordIntervalMs / elapsed;
            int32_t newSkips = (int32_t)((double)s_prevSkipsPerPeriod * ratio);
            if (newSkips < 1)
                newSkips = 1;
            if (newSkips > 10000)
                newSkips = 10000;

            s_prevSkipsPerPeriod = s_skipsPerPeriod;
            s_skipsPerPeriod = newSkips;
        }
    }

    s_lastRecordTimeMs = now;
}

static bool ShouldRecordFrame()
{
    if (++s_recordSkipCounter < s_skipsPerPeriod)
        return false;

    s_recordSkipCounter = 0;
    UpdateRecordFrequency();

    return true;
}

void BrowserProfiler_OnMethodEnter(void *pMethodDesc)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    MethodDesc *pMD = (MethodDesc *)pMethodDesc;

    if (s_topStackFrameIndex + 1 >= MAX_STACK_DEPTH)
    {
        // Shadow stack is full. Stop recording deeper frames but keep
        // counting them so the matching leaves stay balanced.
        s_overflowDepth++;
        return;
    }

    s_topStackFrameIndex++;
    ProfilerStackFrame *frame = &s_profilerStack[s_topStackFrameIndex];
    frame->pMethod = pMD;
    frame->startMs = emscripten_get_now();
    frame->shouldRecord = ShouldRecordFrame();
}

void BrowserProfiler_OnMethodLeave(void *pMethodDesc)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    // Unwind frames that were dropped because the shadow stack was full.
    if (s_overflowDepth > 0)
    {
        s_overflowDepth--;
        return;
    }

    if (s_topStackFrameIndex < 0)
        return;

    // Find the matching frame from the top down. The common case is that the
    // top frame matches (O(1)). Scanning downwards makes the shadow stack
    // self-healing: if some exit path failed to emit a leave (e.g. an unwind
    // route the profiler doesn't hook), the next ancestor leave discards the
    // orphaned frames above it instead of leaking them forever.
    int idx = s_topStackFrameIndex;
    while (idx >= 0 && s_profilerStack[idx].pMethod != (MethodDesc *)pMethodDesc)
        idx--;

    // No matching enter was recorded for this method (filtered out, or an
    // unbalanced leave). Leave the stack untouched.
    if (idx < 0)
        return;

    ProfilerStackFrame *frame = &s_profilerStack[idx];

    if (frame->shouldRecord)
    {
        // Pass the MethodDesc* to JS, which caches the formatted name by
        // pointer and only calls back into SystemJS_GetMethodName()
        // on a cache miss.
        ds_rt_browser_performance_measure(frame->pMethod, frame->startMs);

        // Mark parent frame for recording so the flame chart nests properly.
        if (idx > 0)
            s_profilerStack[idx - 1].shouldRecord = true;
    }

    // Pop the matched frame along with any orphaned frames above it.
    s_topStackFrameIndex = idx - 1;
}

// Formats the name of a MethodDesc* into a freshly malloc'd UTF-8 string.
// Called from JS only on a cache miss; the JS caller owns the returned
// buffer and must free() it. Returns NULL on allocation failure.
extern "C" const char* SystemJS_GetMethodName(void *pMethodDesc)
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    MethodDesc *pMD = (MethodDesc *)pMethodDesc;

    SString methodName;
    TypeString::AppendMethodInternal(methodName, pMD, TypeString::FormatBasic);

    const char *utf8 = methodName.GetUTF8();
    size_t size = strlen(utf8) + 1;
    char *result = (char *)malloc(size);
    if (result != NULL)
        memcpy(result, utf8, size);

    return result;
}

#endif // TARGET_BROWSER && PERFTRACING_DISABLE_THREADS

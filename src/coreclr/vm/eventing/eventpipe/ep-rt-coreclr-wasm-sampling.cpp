// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <eventpipe/ep-rt-config.h>

#ifdef ENABLE_PERFTRACING

#include <eventpipe/ep-types.h>
#include <eventpipe/ep.h>
#include <eventpipe/ep-stack-contents.h>
#include <eventpipe/ep-sample-profiler.h>
#include <eventpipe/ep-rt.h>
#include "threadsuspend.h"

#ifdef TARGET_BROWSER
#include <emscripten.h>
#endif


// State for single-threaded EP sampling profiler.
// On single-threaded WASM, sampling is cooperative: the interpreter calls
// SamplingProfiler_OnSamplepoint() at backward branches (loop iterations)
// and method entry. A skip counter provides a fast path, and when the
// counter expires we check if enough wall-clock time has elapsed to
// justify taking a real sample.

static EventPipeEvent *s_currentSamplingEvent = nullptr;
static Thread *s_currentSamplingThread = nullptr;

// Adaptive sampling state.
// s_skipsPerPeriod is the number of samplepoints to skip between actual
// samples. It is adaptively adjusted so that samples occur approximately
// once per s_desiredSampleIntervalMs.
static double s_desiredSampleIntervalMs = 10.0;
static double s_lastSampleTimeMs = 0.0;
static int32_t s_prevSkipsPerPeriod = 1;
static int32_t s_skipsPerPeriod = 1;
static int32_t s_sampleSkipCounter = 0;

// Returns the current time in milliseconds using the same high-resolution
// timer as EventPipe timestamps (performance.now() on browser WASM).
static double GetCurrentTimeMs()
{
#ifdef TARGET_BROWSER
    return emscripten_get_now();
#else
    return (double)minipal_hires_ticks() * 1000.0 / (double)minipal_hires_tick_frequency();
#endif
}

// Recalculates s_skipsPerPeriod based on how long the last period actually
// took relative to the desired interval. This is the same exponential
// moving average approach used by Mono's ep-rt-mono-runtime-provider.c.
static void UpdateSampleFrequency()
{
    double now = GetCurrentTimeMs();

    if (s_lastSampleTimeMs > 0.0)
    {
        double elapsed = now - s_lastSampleTimeMs;
        if (elapsed > 0.0)
        {
            double ratio = s_desiredSampleIntervalMs / elapsed;
            int32_t newSkips = (int32_t)((double)s_prevSkipsPerPeriod * ratio);
            if (newSkips < 1)
                newSkips = 1;
            if (newSkips > 10000)
                newSkips = 10000;

            s_prevSkipsPerPeriod = s_skipsPerPeriod;
            s_skipsPerPeriod = newSkips;
        }
    }

    s_lastSampleTimeMs = now;
}

#ifndef PERFTRACING_DISABLE_THREADS

// On multi-threaded builds the sample profiler runs on a dedicated
// thread, so these callbacks are no-ops.

void ep_rt_coreclr_sample_profiler_enabled(EventPipeEvent *samplingEvent)
{
}

void ep_rt_coreclr_sample_profiler_session_enabled(void)
{
}

void ep_rt_coreclr_sample_profiler_disabled(void)
{
}

#else // PERFTRACING_DISABLE_THREADS

// The following functions are EP runtime callbacks invoked only on
// single-threaded builds where the regular threaded sample profiler
// cannot run.

void ep_rt_coreclr_sample_profiler_enabled(EventPipeEvent *samplingEvent)
{
    s_currentSamplingEvent = samplingEvent;
    s_currentSamplingThread = GetThread();

    s_desiredSampleIntervalMs = (double)ep_sample_profiler_get_sampling_rate() / 1000000.0;

    s_lastSampleTimeMs = 0.0;
    s_prevSkipsPerPeriod = 1;
    s_skipsPerPeriod = 1;
    s_sampleSkipCounter = 0;
}

void ep_rt_coreclr_sample_profiler_session_enabled(void)
{
    if (s_currentSamplingEvent == nullptr || s_currentSamplingThread == nullptr)
        return;

    EventPipeStackContents stackContents;
    EventPipeStackContents *pStackContents = ep_stack_contents_init(&stackContents);

    uint32_t payloadData = EP_SAMPLE_PROFILER_SAMPLE_TYPE_MANAGED;

    ep_write_sample_profile_event(
        s_currentSamplingThread,
        s_currentSamplingEvent,
        s_currentSamplingThread,
        pStackContents,
        (uint8_t *)&payloadData,
        sizeof(payloadData));

    ep_stack_contents_fini(pStackContents);
}

void ep_rt_coreclr_sample_profiler_disabled(void)
{
    s_currentSamplingEvent = nullptr;
    s_currentSamplingThread = nullptr;
    s_sampleSkipCounter = 0;
    s_skipsPerPeriod = 1;
}

// Called from the interpreter's INTOP_PROF_SAMPLEPOINT handler.
// On single-threaded WASM this is the cooperative sampling entry point.
// On multi-threaded platforms the opcode is never emitted.
extern "C" void SamplingProfiler_OnSamplepoint()
{
    CONTRACTL {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_COOPERATIVE;
    } CONTRACTL_END;

    if (++s_sampleSkipCounter < s_skipsPerPeriod)
        return;

    s_sampleSkipCounter = 0;

    if (s_currentSamplingEvent == nullptr || s_currentSamplingThread == nullptr)
        return;

    UpdateSampleFrequency();

    EventPipeStackContents stackContents;
    EventPipeStackContents *pStackContents = ep_stack_contents_init(&stackContents);

    if (ep_rt_coreclr_walk_managed_stack_for_thread(s_currentSamplingThread, pStackContents)
        && !ep_stack_contents_is_empty(pStackContents))
    {
        uint32_t payloadData = EP_SAMPLE_PROFILER_SAMPLE_TYPE_MANAGED;

        ep_write_sample_profile_event(
            s_currentSamplingThread,
            s_currentSamplingEvent,
            s_currentSamplingThread,
            pStackContents,
            (uint8_t *)&payloadData,
            sizeof(payloadData));
    }

    ep_stack_contents_fini(pStackContents);
}

#endif // PERFTRACING_DISABLE_THREADS

#endif // ENABLE_PERFTRACING

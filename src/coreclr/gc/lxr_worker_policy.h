// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef LXR_WORKER_POLICY_H
#define LXR_WORKER_POLICY_H

#include <cstddef>

// P25 (F4). How many worker threads the collector starts. This is the rule of the Java LXR (mmtk-openjdk 0682434ae725).
//
// ParallelGCThreads is the HotSpot default (Abstract_VM_Version::parallel_worker_threads, mmtk-openjdk
// thirdPartyHeapArguments.cpp:ThirdPartyHeapArguments::initialize passes it on): one worker per processor up to 8 processors,
// then 5 workers for every 8 further processors. 16 processors give 13 workers (Measured in P24).
// ConcGCThreads is scale_concurrent_worker_threads in the same file: (ParallelGCThreads + 2) / 4, at least 1. 13 workers
// give 3 concurrent workers. The concurrent workers are a subset of the parallel workers.
//
// The upper limit is a limit of this port, not of the Java LXR: every worker owns copy buffers and counters.
class LxrWorkerPolicy final
{
public:
    static constexpr size_t MaximumWorkers = 64;

    static constexpr size_t ParallelThreads(size_t processors)
    {
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F4_WORKERS_CONTROL)
        // Control: the constant of the first prototype.
        return processors == 0 ? 1 : 4;
#else
        size_t workers = processors == 0 ? 1 : processors <= 8 ? processors : 8 + (processors - 8) * 5 / 8;
        return workers > MaximumWorkers ? MaximumWorkers : workers;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F4_WORKERS_CONTROL
    }

    static constexpr size_t ConcurrentThreads(size_t parallelThreads)
    {
        size_t workers = (parallelThreads + 2) / 4;
        return workers > 1 ? workers : 1;
    }

    // P26 (F2). The default of this port. It is the Java rule with a floor of 2 trace workers on a machine that has 2 or more
    // workers. DIFFERENT from the Java LXR on purpose: with 1 trace worker (the Java value for 2 to 5 workers) the trace of
    // list suffix does not end on 2 processors (Measured: 1 InitialMark and no FinalMark in 111 pauses, 6.8 s against 5.5 s),
    // and on 4 processors it takes 4.9 s against 4.0 s with 2 trace workers. For 6 or more workers the rule is the Java rule
    // (13 workers give 3).
    static constexpr size_t DefaultConcurrentTraceThreads(size_t parallelThreads)
    {
#if defined(LXR_SIDE_METADATA_TEST_PLATFORM) && defined(LXR_F4B_NO_FLOOR_CONTROL)
        return ConcurrentThreads(parallelThreads);
#else
        size_t floor = parallelThreads < 2 ? parallelThreads : 2;
        size_t java = ConcurrentThreads(parallelThreads);
        return java > floor ? java : floor;
#endif // LXR_SIDE_METADATA_TEST_PLATFORM && LXR_F4B_NO_FLOOR_CONTROL
    }
};

#endif // LXR_WORKER_POLICY_H

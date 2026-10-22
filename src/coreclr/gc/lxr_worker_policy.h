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
};

#endif // LXR_WORKER_POLICY_H

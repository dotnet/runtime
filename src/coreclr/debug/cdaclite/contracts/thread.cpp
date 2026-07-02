// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// thread.cpp
//
// Implementation of the Thread stack-region walk declared in thread.h.
//*****************************************************************************

#include "thread.h"
#include "runtimetypes.h"

#include <set>

namespace cdac
{
namespace contracts
{
    namespace
    {
        // Global pointing at the ThreadStore*: &ThreadStore::s_pThreadStore.
        const char* const GlobalThreadStore = "ThreadStore";

        const int MaxThreads = 100000; // guard against corrupt thread lists
    }

    int EnumerateThreadRegions(const Target& target, RegionCallback sink, void* sinkContext)
    {
        // ThreadStore global is a pointer-to-pointer: deref once to get the ThreadStore.
        uint64_t threadStoreAddr = 0;
        if (!target.TryReadGlobalPointer(GlobalThreadStore, threadStoreAddr) || threadStoreAddr == 0)
        {
            return -1;
        }

        data::ThreadStore threadStore;
        if (!target.TryRead(threadStoreAddr, threadStore))
        {
            return -1;
        }

        std::set<uint64_t> visited;
        int count = 0;
        uint64_t threadAddr = threadStore.FirstThreadLink; // head Thread*

        for (int i = 0; threadAddr != 0 && i < MaxThreads; i++)
        {
            if (!visited.insert(threadAddr).second)
            {
                break; // cycle
            }

            data::Thread thread;
            if (!target.TryRead(threadAddr, thread))
            {
                break;
            }

            // Stack grows down: CachedStackLimit is the low address, CachedStackBase
            // the high address. Report the committed stack range.
            if (thread.CachedStackBase > thread.CachedStackLimit && thread.CachedStackLimit != 0)
            {
                sink(sinkContext, "thread-stack", thread.CachedStackLimit,
                     thread.CachedStackBase - thread.CachedStackLimit);
                count++;
            }

            threadAddr = thread.LinkNext;
        }

        return count;
    }
}
} // namespace contracts

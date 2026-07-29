// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// loaderheaps.cpp
//
// Implementation of the LoaderAllocator heaps walk declared in loaderheaps.h.
//*****************************************************************************

#include "loaderheaps.h"
#include "loader.h" // ForEachModule
#include "runtimetypes.h"

#include <set>

namespace cdac
{
namespace contracts
{
    namespace
    {
        // Global pointing at the SystemDomain*: cdac_data<SystemDomain>::SystemDomainPtr.
        const char* const GlobalSystemDomain = "SystemDomain";

        const int MaxHeapBlocks = 1000000;

        struct CollectState
        {
            const Target* target;
            std::set<uint64_t>* allocators;
        };

        // ForEachModule callback: record each Module's LoaderAllocator.
        void CollectModuleAllocator(void* context, uint64_t moduleAddr)
        {
            CollectState* state = (CollectState*)context;
            data::Module module;
            if (state->target->TryRead(moduleAddr, module) && module.LoaderAllocator != 0)
            {
                state->allocators->insert(module.LoaderAllocator);
            }
        }

        // Walks one LoaderHeap's block list, reporting each block's committed range.
        int WalkLoaderHeap(const Target& target, uint64_t heapAddr,
                           std::set<uint64_t>& visitedBlocks, RegionCallback sink, void* sinkContext)
        {
            if (heapAddr == 0)
            {
                return 0;
            }

            data::LoaderHeap heap;
            if (!target.TryRead(heapAddr, heap))
            {
                return 0;
            }

            int count = 0;
            uint64_t block = heap.FirstBlock;
            for (int i = 0; block != 0 && i < MaxHeapBlocks; i++)
            {
                if (!visitedBlocks.insert(block).second)
                {
                    break; // cycle
                }

                data::LoaderHeapBlock heapBlock;
                if (!target.TryRead(block, heapBlock))
                {
                    break;
                }

                if (heapBlock.VirtualAddress != 0 && heapBlock.VirtualSize != 0)
                {
                    sink(sinkContext, "loader-heap", heapBlock.VirtualAddress, heapBlock.VirtualSize);
                    count++;
                }

                block = heapBlock.Next;
            }
            return count;
        }

        int WalkAllocator(const Target& target, uint64_t allocatorAddr,
                          std::set<uint64_t>& visitedBlocks, RegionCallback sink, void* sinkContext)
        {
            data::LoaderAllocator allocator;
            if (!target.TryRead(allocatorAddr, allocator))
            {
                return 0;
            }

            const uint64_t heaps[] = {
                allocator.HighFrequencyHeap,
                allocator.LowFrequencyHeap,
                allocator.StaticsHeap,
                allocator.StubHeap,
                allocator.ExecutableHeap,
                allocator.FixupPrecodeHeap,
                allocator.NewStubPrecodeHeap,
                allocator.DynamicHelpersStubHeap,
            };

            int count = 0;
            for (uint64_t heap : heaps)
            {
                count += WalkLoaderHeap(target, heap, visitedBlocks, sink, sinkContext);
            }
            return count;
        }
    }

    int EnumerateLoaderHeapRegions(const Target& target, RegionCallback sink, void* sinkContext)
    {
        std::set<uint64_t> allocators;

        // The global loader allocator is embedded in the SystemDomain.
        uint64_t systemDomainAddr = 0;
        if (target.TryReadGlobalPointer(GlobalSystemDomain, systemDomainAddr) && systemDomainAddr != 0)
        {
            data::SystemDomain systemDomain;
            if (target.TryRead(systemDomainAddr, systemDomain) && systemDomain.GlobalLoaderAllocator != 0)
            {
                allocators.insert(systemDomain.GlobalLoaderAllocator);
            }
        }

        // Per-module loader allocators (collectible assemblies each have their own).
        CollectState collect;
        collect.target = &target;
        collect.allocators = &allocators;
        ForEachModule(target, &CollectModuleAllocator, &collect);

        if (allocators.empty())
        {
            return -1;
        }

        std::set<uint64_t> visitedBlocks;
        int total = 0;
        for (uint64_t allocatorAddr : allocators)
        {
            total += WalkAllocator(target, allocatorAddr, visitedBlocks, sink, sinkContext);
        }
        return total;
    }
}
} // namespace contracts

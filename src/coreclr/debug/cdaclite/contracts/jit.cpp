// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// jit.cpp
//
// Implementation of the JIT code-heap walk declared in jit.h.
//*****************************************************************************

#include "jit.h"
#include "runtimetypes.h"

#include <set>

namespace cdac
{
namespace contracts
{
    namespace
    {
        // Pointer-to-pointer global: &g_pEEJitManager (deref once for the manager).
        const char* const GlobalEEJitManager = "EEJitManagerAddress";

        const int MaxCodeHeaps = 1000000;
    }

    int EnumerateJitCodeRegions(const Target& target, RegionCallback sink, void* sinkContext)
    {
        // ReadGlobalPointer = ReadPointer(ReadGlobalPointer(name)) -> the EEJitManager.
        uint64_t jitManagerAddr = 0;
        if (!target.TryReadGlobalPointer(GlobalEEJitManager, jitManagerAddr) || jitManagerAddr == 0)
        {
            return -1;
        }

        data::EEJitManager jitManager;
        if (!target.TryRead(jitManagerAddr, jitManager))
        {
            return -1;
        }

        std::set<uint64_t> visited;
        int count = 0;
        uint64_t node = jitManager.AllCodeHeaps;

        for (int i = 0; node != 0 && i < MaxCodeHeaps; i++)
        {
            if (!visited.insert(node).second)
            {
                break; // cycle
            }

            data::CodeHeapListNode heap;
            if (!target.TryRead(node, heap))
            {
                break;
            }

            if (heap.StartAddress != 0 && heap.EndAddress > heap.StartAddress)
            {
                sink(sinkContext, "jit-code", heap.StartAddress, heap.EndAddress - heap.StartAddress);
                count++;
            }

            // The cDAC's GetCodeHeapInfos reads node.Heap -> CodeHeap (HeapType) and then the
            // Loader/Host code-heap struct. Read them so they are captured (EnumMem). Both overlay
            // the same address; reading both avoids depending on the HeapType enum value.
            if (heap.Heap != 0)
            {
                data::CodeHeap codeHeap;
                target.TryRead(heap.Heap, codeHeap);
                data::LoaderCodeHeap loaderCodeHeap;
                target.TryRead(heap.Heap, loaderCodeHeap);
                data::HostCodeHeap hostCodeHeap;
                target.TryRead(heap.Heap, hostCodeHeap);
            }

            node = heap.Next;
        }

        return count;
    }
}
} // namespace contracts

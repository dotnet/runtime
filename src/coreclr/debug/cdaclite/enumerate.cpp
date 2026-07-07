// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// enumerate.cpp
//
// The memory enumeration: CDacLite::EnumMemoryRegions drives the cdac-lite
// contracts (GC, threads, modules, handles, JIT, loader heaps, ...) to select
// the managed memory a crash dump should include, and the region sinks that
// forward selected regions to the ICLRDataEnumMemoryRegions callback.
//*****************************************************************************

#include "cdaclite.h"
#include "datatarget.h"

#include <string>
#include <cstring>

#include "data/datadescriptor.h"
#include "data/target.h"
#include "contracts/gc.h"
#include "contracts/thread.h"
#include "contracts/loader.h"
#include "contracts/loaderheaps.h"
#include "contracts/handles.h"
#include "contracts/jit.h"
#include "contracts/statics.h"
#include "contracts/syncblock.h"
#include "contracts/stresslog.h"
#include "contracts/interop.h"
#include "contracts/stackscan.h"
#include "contracts/bootstrap.h"

namespace cdac
{
    // Region sink: a contract reported a memory region [start, start+size). Forward it to the
    // COM callback and log it.
    void CDacLite::RegionSinkThunk(void* context, const char* kind, uint64_t start, uint64_t size)
    {
        RegionSinkState* state = (RegionSinkState*)context;
        state->callback->EnumMemoryRegion((CLRDATA_ADDRESS)start, (ULONG32)size);
        state->count++;
        state->owner->Log(state->callback, "region %s [0x%llx, 0x%llx)",
            kind, (unsigned long long)start, (unsigned long long)(start + size));
    }

    // EnumMem observer (DAC DPTR::EnumMem() analog): records the metadata structures a walk
    // traverses. Forwarded to the same COM callback, but not logged per-region (there are many
    // small structs) -- only counted.
    void CDacLite::EnumMemThunk(void* context, uint64_t address, uint32_t size)
    {
        RegionSinkState* state = (RegionSinkState*)context;
        state->callback->EnumMemoryRegion((CLRDATA_ADDRESS)address, (ULONG32)size);
        state->count++;
    }

    // Memory-enumerator dispatch: a uniform table of subsystem enumerators, each of which decides
    // what to emit based on the Target's dump tier. The driver (EnumMemoryRegions) just runs them
    // all in order.
    namespace
    {
        // Execution/code enumerator: one entry point that chooses its strategy by dump tier. A Heap
        // dump captures the whole process, so bulk-emit every JIT code heap + loader heap. A Normal
        // dump captures only what a stack walk reaches, so do the conservative stack scan instead
        // (which scales to large programs where the code/loader heaps are huge).
        int EnumerateCodeRegions(const cdac::Target& target, cdac::contracts::RegionCallback sink, void* sinkContext)
        {
            if (target.Tier() == cdac::DumpTier::Heap)
            {
                int jit = cdac::contracts::EnumerateJitCodeRegions(target, sink, sinkContext);
                int loaderHeaps = cdac::contracts::EnumerateLoaderHeapRegions(target, sink, sinkContext);
                if (jit < 0 || loaderHeaps < 0)
                {
                    return -1;
                }
                return jit + loaderHeaps;
            }
            return cdac::contracts::EnumerateStackScanRegions(target, sink, sinkContext);
        }

        // The memory enumerators, run in order for every dump. Each is uniform -- it takes the
        // Target (which carries the dump tier, the runtime base, and the contract-descriptor
        // address) plus the region sink -- and decides internally what to do for the current tier
        // (heap-only enumerators return 0 in a Normal dump). Adding a subsystem is one table entry.
        struct MemoryEnumerator
        {
            const char* name;
            int (*enumerate)(const cdac::Target&, cdac::contracts::RegionCallback, void*);
        };

        const MemoryEnumerator s_enumerators[] = {
            { "bootstrap",  cdac::contracts::EnumerateBootstrapRegions },
            { "thread",     cdac::contracts::EnumerateThreadRegions },
            { "module",     cdac::contracts::EnumerateModuleRegions },
            { "code",       EnumerateCodeRegions },
            { "gc",         cdac::contracts::EnumerateGCHeapRegions },
            { "handle",     cdac::contracts::EnumerateHandleRegions },
            { "syncblock",  cdac::contracts::EnumerateSyncBlockRegions },
            { "stresslog",  cdac::contracts::EnumerateStressLogRegions },
            { "interop",    cdac::contracts::EnumerateInteropRegions },
            { "descriptor", cdac::contracts::EnumerateStaticRegions },
        };
    }

    HRESULT STDMETHODCALLTYPE CDacLite::EnumMemoryRegions(
        ICLRDataEnumMemoryRegionsCallback* callback, ULONG32 miniDumpFlags, CLRDataEnumMemoryFlags clrFlags)
    {
        UNREFERENCED_PARAMETER(clrFlags);

        Log(callback, "EnumMemoryRegions: contract descriptor @ 0x%llx (miniDumpFlags=0x%x)",
            (unsigned long long)m_contractDescriptorAddr, (unsigned)miniDumpFlags);

        // Read + parse the contract descriptor, recursively merging sub-descriptors
        // (e.g. the GC descriptor) and resolving all indirect globals.
        cdac::DataDescriptor descriptor;
        std::string error;
        if (!descriptor.Load(&cdac::ReadFromDataTarget, m_target, m_contractDescriptorAddr, error))
        {
            Log(callback, "EnumMemoryRegions: descriptor load failed: %s", error.c_str());
            return E_FAIL;
        }

        Log(callback, "EnumMemoryRegions: loaded %zu types, %zu globals, %zu contracts",
            descriptor.Types().size(), descriptor.Globals().size(), descriptor.Contracts().size());

        // Build the Target (globals are all resolved to absolute values). It carries the dump tier,
        // the runtime module base, and the contract-descriptor address so every enumerator has the
        // same (Target, sink) signature and can self-select its behavior for the current tier.
        cdac::Target target(&descriptor, &cdac::ReadFromDataTarget, m_target);
        target.SetContractDescriptorAddr(m_contractDescriptorAddr);
        target.SetClrBase(m_clrBase);

        // Dump tier: HEAP2 (MiniDumpWithPrivateReadWriteMemory) requests the full GC heap +
        // heap-side structures. A Normal dump (no HEAP2 flag) needs only what a stack walk reaches
        // -- stacks, code, and method metadata -- so the heap-only enumerators return 0.
        cdac::DumpTier tier = (miniDumpFlags & 0x200 /*MiniDumpWithPrivateReadWriteMemory*/) != 0
            ? cdac::DumpTier::Heap
            : cdac::DumpTier::Normal;
        target.SetTier(tier);
        Log(callback, "EnumMemoryRegions: tier = %s",
            tier == cdac::DumpTier::Heap ? "heap" : "normal (stack walk)");

        RegionSinkState sinkState = { this, callback, 0 };

        // Enable implicit metadata enumeration: every structure an enumerator reads via TryRead is
        // captured (the cdac-lite analog of the DAC recording each DPTR it dereferences).
        target.SetEnumMemSink(&CDacLite::EnumMemThunk, &sinkState);

        // Run every enumerator in order. Each decides -- from the Target's tier -- what to emit
        // (heap-only enumerators return 0 in a Normal dump); adding a subsystem is one table entry.
        for (const MemoryEnumerator& e : s_enumerators)
        {
            int n = e.enumerate(target, &CDacLite::RegionSinkThunk, &sinkState);
            if (n < 0)
            {
                Log(callback, "EnumMemoryRegions: %s enumerator skipped (root not found)", e.name);
            }
            else
            {
                Log(callback, "EnumMemoryRegions: %s enumerator reported %d region(s)", e.name, n);
            }
        }

        return S_OK;
    }
}

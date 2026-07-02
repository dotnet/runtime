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

        // Build the Target (globals are all resolved to absolute values).
        cdac::Target target(&descriptor, &cdac::ReadFromDataTarget, m_target);

        RegionSinkState sinkState = { this, callback, 0 };

        // Enable implicit metadata enumeration: every structure the walks read is now captured
        // (the cdac-lite analog of the DAC recording each DPTR it dereferences).
        target.SetEnumMemSink(&CDacLite::EnumMemThunk, &sinkState);

        // Dump tier: HEAP2 (MiniDumpWithPrivateReadWriteMemory) requests the full GC heap +
        // heap-side structures. A Normal dump (no HEAP2 flag) needs only what a stack walk
        // reaches -- stacks, code, and method metadata -- so the GC heap and the heap-only
        // contracts (handles, sync blocks, stress log, COM interop) are skipped.
        cdac::DumpTier tier = (miniDumpFlags & 0x200 /*MiniDumpWithPrivateReadWriteMemory*/) != 0
            ? cdac::DumpTier::Heap
            : cdac::DumpTier::Normal;
        target.SetTier(tier);
        bool heapTier = (tier == cdac::DumpTier::Heap);
        Log(callback, "EnumMemoryRegions: tier = %s", heapTier ? "heap" : "normal (stack walk)");

        if (heapTier)
        {
            int gcRegions = cdac::contracts::EnumerateGCHeapRegions(target, &CDacLite::RegionSinkThunk, &sinkState);
            if (gcRegions < 0)
            {
                Log(callback, "EnumMemoryRegions: GC walk skipped (GC type unknown)");
            }
            else
            {
                Log(callback, "EnumMemoryRegions: GC walk reported %d region(s)", gcRegions);
            }
        }

        // Walk the managed threads and report their stack ranges.
        int threadRegions = cdac::contracts::EnumerateThreadRegions(target, &CDacLite::RegionSinkThunk, &sinkState);
        if (threadRegions < 0)
        {
            Log(callback, "EnumMemoryRegions: thread walk skipped (ThreadStore not found)");
        }
        else
        {
            Log(callback, "EnumMemoryRegions: thread walk reported %d region(s)", threadRegions);
        }

        // Walk loaded modules and capture in-memory symbol streams (file-backed images come from disk).
        int moduleRegions = cdac::contracts::EnumerateModuleRegions(target, &CDacLite::RegionSinkThunk, &sinkState);
        if (moduleRegions < 0)
        {
            Log(callback, "EnumMemoryRegions: module walk skipped (AppDomain not found)");
        }
        else
        {
            Log(callback, "EnumMemoryRegions: module walk reported %d region(s)", moduleRegions);
        }

        // Walk the GC handle table and report its segments (roots storage). Heap tier only.
        if (heapTier)
        {
            int handleRegions = cdac::contracts::EnumerateHandleRegions(target, &CDacLite::RegionSinkThunk, &sinkState);
            if (handleRegions < 0)
            {
                Log(callback, "EnumMemoryRegions: handle walk skipped (handle table not found)");
            }
            else
            {
                Log(callback, "EnumMemoryRegions: handle walk reported %d region(s)", handleRegions);
            }
        }

        // Code + method metadata for stack walks. Heap tier bulk-emits every JIT code heap and
        // loader heap (fine when the whole heap is captured anyway). Normal tier instead does a
        // conservative stack scan, capturing only the code + MethodDesc reachable from pointers on
        // the thread stacks -- this scales to large programs where the code/loader heaps are huge.
        if (heapTier)
        {
            int jitRegions = cdac::contracts::EnumerateJitCodeRegions(target, &CDacLite::RegionSinkThunk, &sinkState);
            if (jitRegions < 0)
            {
                Log(callback, "EnumMemoryRegions: JIT walk skipped (EEJitManager not found)");
            }
            else
            {
                Log(callback, "EnumMemoryRegions: JIT walk reported %d region(s)", jitRegions);
            }

            int loaderHeapRegions = cdac::contracts::EnumerateLoaderHeapRegions(target, &CDacLite::RegionSinkThunk, &sinkState);
            if (loaderHeapRegions < 0)
            {
                Log(callback, "EnumMemoryRegions: loader-heap walk skipped (allocators not found)");
            }
            else
            {
                Log(callback, "EnumMemoryRegions: loader-heap walk reported %d region(s)", loaderHeapRegions);
            }
        }
        else
        {
            int scannedMethods = cdac::contracts::EnumerateStackScanRegions(target, &CDacLite::RegionSinkThunk, &sinkState);
            if (scannedMethods < 0)
            {
                Log(callback, "EnumMemoryRegions: stack scan skipped (code range map not found)");
            }
            else
            {
                Log(callback, "EnumMemoryRegions: stack scan captured %d method(s)", scannedMethods);
            }
        }

        // Heap-only structures: sync-block table, stress log, and COM interop. A Normal
        // (stack-walk) dump does not need these.
        if (heapTier)
        {
            int syncBlockRegions = cdac::contracts::EnumerateSyncBlockRegions(target);
            if (syncBlockRegions < 0)
            {
                Log(callback, "EnumMemoryRegions: sync-block walk skipped (sync table not found)");
            }
            else
            {
                Log(callback, "EnumMemoryRegions: sync-block walk captured %d in-use block(s)", syncBlockRegions);
            }

            int stressLogChunks = cdac::contracts::EnumerateStressLogRegions(target);
            if (stressLogChunks < 0)
            {
                Log(callback, "EnumMemoryRegions: stress-log walk skipped (stress log not found)");
            }
            else
            {
                Log(callback, "EnumMemoryRegions: stress-log walk captured %d chunk(s)", stressLogChunks);
            }

            int interopRegions = cdac::contracts::EnumerateInteropRegions(target);
            if (interopRegions < 0)
            {
                Log(callback, "EnumMemoryRegions: interop walk skipped (RCW cleanup list not found)");
            }
            else
            {
                Log(callback, "EnumMemoryRegions: interop walk captured %d RCW(s)", interopRegions);
            }
        }

        // Report the contract self-description (descriptor + JSON + pointer_data + the global
        // storage it references) so a contract tool can bootstrap from the dump alone. cdac-lite
        // is a DAC replacement, so no DAC globals table.
        int staticRegions = cdac::contracts::EnumerateStaticRegions(target, m_contractDescriptorAddr,
            &CDacLite::RegionSinkThunk, &sinkState);
        Log(callback, "EnumMemoryRegions: contract-bootstrap reported %d region(s)", staticRegions);

        return S_OK;
    }
}

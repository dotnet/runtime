// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "lxr_write_barrier.h"

#if defined(_MSC_VER)
#include <intrin.h>
#else
#include <cpuid.h>
#endif

extern "C" LxrWriteBarrierGlobals g_lxr_write_barrier_globals = {};

static uintptr_t s_avLocations[] =
{
    reinterpret_cast<uintptr_t>(&LxrCheckedAssignRefAVLocation),
    reinterpret_cast<uintptr_t>(&LxrAssignRefMetadataAVLocation),
    reinterpret_cast<uintptr_t>(&LxrAssignRefAVLocation),
    reinterpret_cast<uintptr_t>(&LxrBarrierSlowAVLocation),
    0,
};

static LxrWriteBarrierPatchable s_patchable[] =
{
    { &LxrPatchable_MetadataBase, &g_lxr_write_barrier_globals.metadata_base },
    { &LxrPatchable_HeapLow, &g_lxr_write_barrier_globals.heap_low },
    { &LxrPatchable_HeapSize, &g_lxr_write_barrier_globals.heap_size },
    { &LxrPatchable_SlowPath, &g_lxr_write_barrier_globals.slow_path },
    { &LxrPatchable_XsaveMask, &g_lxr_write_barrier_globals.xsave_mask },
    { nullptr, nullptr },
};

void LxrWriteBarrierGetBlockInfo(LxrWriteBarrierBlockInfo* info)
{
    // Both entries cover the whole block: the runtime copies [first entry, end of the larger size), and the slow
    // path and the slots must be in the copy.
    uint8_t* checked = reinterpret_cast<uint8_t*>(&LxrCheckedAssignRef);
    uint8_t* assign = reinterpret_cast<uint8_t*>(&LxrAssignRef);
    info->checked_assign_ref = checked;
    info->checked_assign_ref_size = static_cast<size_t>(&LxrWriteBarrierBlock_End - checked);
    info->assign_ref = assign;
    info->assign_ref_size = static_cast<size_t>(&LxrWriteBarrierBlock_End - assign);
    info->av_locations = s_avLocations;
    info->patchable = s_patchable;
}

uintptr_t LxrWriteBarrierComputeXsaveMask()
{
    // x87 (1), SSE (2), AVX (4), opmask (32), ZMM_Hi256 (64), Hi16_ZMM (128). The size of the xsave frame in the
    // assembly (2816 bytes) is chosen for exactly this set.
    const uint64_t SavedComponents = 0xE7;
    uint32_t eax, ebx, ecx, edx;
#if defined(_MSC_VER)
    int regs[4];
    __cpuid(regs, 1);
    ecx = static_cast<uint32_t>(regs[2]);
    (void)eax; (void)ebx; (void)edx;
#else
    if (!__get_cpuid(1, &eax, &ebx, &ecx, &edx))
    {
        return 0;
    }
#endif
    const uint32_t Xsave = 1u << 26;
    const uint32_t OsXsave = 1u << 27;
    if ((ecx & (Xsave | OsXsave)) != (Xsave | OsXsave))
    {
        return 0;
    }

    uint64_t xcr0;
#if defined(_MSC_VER)
    xcr0 = _xgetbv(0);
#else
    uint32_t low, high;
    __asm__ volatile("xgetbv" : "=a"(low), "=d"(high) : "c"(0));
    xcr0 = (static_cast<uint64_t>(high) << 32) | low;
#endif
    return static_cast<uintptr_t>(xcr0 & SavedComponents);
}

void LxrWriteBarrierSetGlobals(
    uintptr_t metadataBase, uintptr_t heapLow, uintptr_t heapSize, void* slowPath, uintptr_t xsaveMask)
{
    g_lxr_write_barrier_globals.metadata_base = metadataBase;
    g_lxr_write_barrier_globals.heap_low = heapLow;
    g_lxr_write_barrier_globals.heap_size = heapSize;
    g_lxr_write_barrier_globals.slow_path = reinterpret_cast<uintptr_t>(slowPath);
    g_lxr_write_barrier_globals.xsave_mask = xsaveMask;
}

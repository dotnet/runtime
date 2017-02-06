// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "gcheaputilities.h"

// These globals are variables used within the GC and maintained
// by the EE for use in write barriers. It is the responsibility
// of the GC to communicate updates to these globals to the EE through
// GCToEEInterface::StompWriteBarrierResize and GCToEEInterface::StompWriteBarrierEphemeral.
GPTR_IMPL_INIT(uint32_t, g_card_table,      nullptr);
GPTR_IMPL_INIT(uint8_t,  g_lowest_address,  nullptr);
GPTR_IMPL_INIT(uint8_t,  g_highest_address, nullptr);
uint8_t* g_ephemeral_low  = (uint8_t*)1;
uint8_t* g_ephemeral_high = (uint8_t*)~0;

// This is the global GC heap, maintained by the VM.
GPTR_IMPL(IGCHeap, g_pGCHeap);

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

uint8_t* g_sw_ww_table = nullptr;
bool g_sw_ww_enabled_for_gc_heap = false;

#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
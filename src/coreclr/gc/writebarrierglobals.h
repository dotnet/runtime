// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef GC_WRITEBARRIERGLOBALS_H
#define GC_WRITEBARRIERGLOBALS_H

#include <stdint.h>

// These GC-owned globals are consumed directly by static write barriers and
// retained under their historical names for ABI and DAC compatibility.
#ifdef DACCESS_COMPILE
GPTR_DECL(uint32_t, g_card_table);
GPTR_DECL(uint8_t, g_lowest_address);
GPTR_DECL(uint8_t, g_highest_address);
#else
extern "C"
{
extern uint32_t* g_card_table;
extern uint8_t* g_lowest_address;
extern uint8_t* g_highest_address;
}
#endif // DACCESS_COMPILE

extern "C"
{
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
extern uint32_t* g_card_bundle_table;
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
extern uint8_t* g_ephemeral_low;
extern uint8_t* g_ephemeral_high;
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
extern uint8_t* g_write_watch_table;
extern bool g_sw_ww_enabled_for_gc_heap;
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
}

#endif // GC_WRITEBARRIERGLOBALS_H

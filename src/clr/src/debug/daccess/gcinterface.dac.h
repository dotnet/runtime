// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#ifndef __DACCESS_GCINTERFACE_DAC_H__
#define __DACCESS_GCINTERFACE_DAC_H__

#include "../../gc/gcinterface.dac.h"

// The following six function prototypes are for functions that are used by the DAC
// to inspect the handle table in detail. The semantics of these functions MUST be
// versioned along with the rest of this interface - any changes in semantics
// must be accompanied with a major versino bump.
// 
// Please do not add any additional functions to this list; we'd like to keep it
// to an absolute minimum.
#ifdef DACCESS_COMPILE
// for DAC builds, OBJECTHANDLE is a uintptr_t.
GC_DAC_VISIBLE
OBJECTREF GetDependentHandleSecondary(OBJECTHANDLE handle);

GC_DAC_VISIBLE_NO_MANGLE
void HndScanHandlesForGC(
    DPTR(dac_handle_table) hTable,
    HANDLESCANPROC scanProc,
    uintptr_t param1,
    uintptr_t param2,
    const uint32_t *types,
    uint32_t typeCount,
    uint32_t condemned,
    uint32_t maxGen,
    uint32_t flags);

GC_DAC_VISIBLE_NO_MANGLE
void HndEnumHandles(
    DPTR(dac_handle_table) hTable,
    const uint32_t *puType,
    uint32_t uTypeCount,
    HANDLESCANPROC pfnEnum,
    uintptr_t lParam1,
    uintptr_t lParam2,
    bool fAsync);

GC_DAC_VISIBLE
OBJECTREF HndFetchHandle(OBJECTHANDLE handle);

GC_DAC_VISIBLE
uintptr_t HndGetHandleExtraInfo(OBJECTHANDLE handle);

#endif // DACCESS_COMPILE

#endif // __DACCESS_GCINTERFACE_DAC_H__

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "gcheaputilities.h"
#include "appdomain.hpp"


// These globals are variables used within the GC and maintained
// by the EE for use in write barriers. It is the responsibility
// of the GC to communicate updates to these globals to the EE through
// GCToEEInterface::StompWriteBarrierResize and GCToEEInterface::StompWriteBarrierEphemeral.
GPTR_IMPL_INIT(uint32_t, g_card_table,      nullptr);
GPTR_IMPL_INIT(uint8_t,  g_lowest_address,  nullptr);
GPTR_IMPL_INIT(uint8_t,  g_highest_address, nullptr);
GVAL_IMPL_INIT(GCHeapType, g_heap_type,     GC_HEAP_INVALID);
uint8_t* g_ephemeral_low  = (uint8_t*)1;
uint8_t* g_ephemeral_high = (uint8_t*)~0;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
uint32_t* g_card_bundle_table = nullptr;
#endif

// This is the global GC heap, maintained by the VM.
GPTR_IMPL(IGCHeap, g_pGCHeap);

IGCHandleTable* g_pGCHandleTable = nullptr;

GcDacVars g_gc_dac_vars;
GPTR_IMPL(GcDacVars, g_gcDacGlobals);

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

uint8_t* g_sw_ww_table = nullptr;
bool g_sw_ww_enabled_for_gc_heap = false;

#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

gc_alloc_context g_global_alloc_context = {};

// Debug-only validation for handle.
void ValidateHandleAndAppDomain(OBJECTHANDLE handle)
{
#ifdef _DEBUG_IMPL
    OBJECTREF objRef = ObjectToOBJECTREF(*(Object**)handle);
    VALIDATEOBJECTREF(objRef);

    IGCHandleTable *pHandleTable = GCHandleTableUtilities::GetGCHandleTable();

    void* handleTable = pHandleTable->GetHandleTableForHandle(handle);
    DWORD context = (DWORD)pHandleTable->GetHandleTableContext(handleTable);

    ADIndex appDomainIndex = ADIndex(context);
    AppDomain *domain = SystemDomain::GetAppDomainAtIndex(appDomainIndex);

    // Access to a handle in an unloaded domain is not allowed
    assert(domain != nullptr);
    assert(!domain->NoAccessToHandleTable());

#if CHECK_APP_DOMAIN_LEAKS
    if (g_pConfig->AppDomainLeaks() && objRef != NULL)
    {
        if (appDomainIndex.m_dwIndex)
        {
            objRef->TryAssignAppDomain(domain);
        }
        else
        {
            objRef->TrySetAppDomainAgile();
        }
    }
#endif // CHECK_APP_DOMAIN_LEAKS
#endif // _DEBUG_IMPL
}

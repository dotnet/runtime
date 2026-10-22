// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gcheaputilities.h"
#include "gchandleutilities.h"

#include "gceventstatus.h"
#include "gcinterface.h"

// This is the global GC heap, maintained by the VM.
GPTR_IMPL(IGCHeap, g_pGCHeap);

// These globals are variables used within the GC and maintained
// by the EE for use in write barriers. It is the responsibility
// of the GC to communicate updates to these globals to the EE through
// GCToEEInterface::StompWriteBarrier.
GPTR_IMPL_INIT(uint32_t, g_card_table,      nullptr);
GPTR_IMPL_INIT(uint8_t,  g_lowest_address,  nullptr);
GPTR_IMPL_INIT(uint8_t,  g_highest_address, nullptr);
GVAL_IMPL_INIT(GCHeapType, g_heap_type,     GC_HEAP_INVALID);
uint8_t* g_ephemeral_low  = (uint8_t*)1;
uint8_t* g_ephemeral_high = (uint8_t*)~0;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
uint32_t* g_card_bundle_table = nullptr;
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
uint8_t* g_write_watch_table = nullptr;
bool g_sw_ww_enabled_for_gc_heap = false;
#endif

IGCHandleManager* g_pGCHandleManager = nullptr;
IGCHandleStore* g_pGlobalHandleStore = nullptr;

GcDacVars g_gc_dac_vars;
GPTR_IMPL(GcDacVars, g_gcDacGlobals);

// GC entrypoints for the linked-in GC. These symbols are invoked
// directly if we are not using a standalone GC.
extern "C" HRESULT LOCALGC_CALLCONV GC_Initialize(
    /* In  */ IGCToCLR* clrToGC,
    /* Out */ IGCHeap** gcHeap,
    /* Out */ IGCHandleManager** gcHandleManager,
    /* Out */ GcDacVars* gcDacVars
);

#ifndef DACCESS_COMPILE

HRESULT InitializeGCSelector();

HRESULT GCHeapUtilities::InitializeGC()
{
    return InitializeGCSelector();
}

HRESULT InitializeDefaultGC()
{
    return GCHeapUtilities::InitializeDefaultGC();
}

namespace
{
Object** LOCALGC_CALLCONV GetLoaderAllocatorObjectSlotForGC(MethodTable* methodTable)
{
    UNREFERENCED_PARAMETER(methodTable);
    return nullptr;
}
}

HRESULT GCHeapUtilities::ConfigureObjectReferenceEnumeration(IGCHeap* gcHeap)
{
    ObjectReferenceEnumerationParameters* parameters =
        gcHeap->GetObjectReferenceEnumerationParameters();
    if ((parameters == nullptr) ||
        (parameters->request_status != ObjectReferenceEnumerationRequestStatus::NotProcessed))
    {
        if (parameters != nullptr)
        {
            parameters->request_status = ObjectReferenceEnumerationRequestStatus::Unsupported;
        }
        return E_FAIL;
    }

    if (parameters->get_loader_allocator_object_slot != nullptr)
    {
        parameters->request_status = ObjectReferenceEnumerationRequestStatus::Unsupported;
        return E_INVALIDARG;
    }

    switch (parameters->request)
    {
        case ObjectReferenceEnumerationRequest::Disabled:
            parameters->request_status = ObjectReferenceEnumerationRequestStatus::Accepted;
            return S_OK;

        case ObjectReferenceEnumerationRequest::Enabled:
            parameters->get_loader_allocator_object_slot =
                GetLoaderAllocatorObjectSlotForGC;
            parameters->request_status = ObjectReferenceEnumerationRequestStatus::Accepted;
            return S_OK;

        default:
            parameters->request_status = ObjectReferenceEnumerationRequestStatus::Unsupported;
            return E_INVALIDARG;
    }
}

namespace
{
constexpr uint32_t ObjectHeaderBitsSupportedAtomicOperations =
    static_cast<uint32_t>(ObjectHeaderBitsAtomicOperation::Load) |
    static_cast<uint32_t>(ObjectHeaderBitsAtomicOperation::CompareExchange) |
    static_cast<uint32_t>(ObjectHeaderBitsAtomicOperation::Store) |
    static_cast<uint32_t>(ObjectHeaderBitsAtomicOperation::Wait);

void ClearObjectHeaderBitsRuntimeOutputs(ObjectHeaderBitsParameters* parameters)
{
    parameters->object_byte_offset = 0;
    parameters->storage_word_size = 0;
    parameters->bit_mask = 0;
    parameters->bit_shift = 0;
    parameters->granted_protocol = ObjectHeaderBitsProtocol::None;
    parameters->granted_atomic_operations = 0;
    parameters->granted_memory_order = ObjectHeaderBitsMemoryOrder::None;
    parameters->clear_state = 0;
    parameters->invalid_state = 0;
    parameters->transition_state = 0;
    parameters->published_state = 0;
}

bool ObjectHeaderBitsRuntimeOutputsAreZero(const ObjectHeaderBitsParameters* parameters)
{
    return
        (parameters->object_byte_offset == 0) &&
        (parameters->storage_word_size == 0) &&
        (parameters->bit_mask == 0) &&
        (parameters->bit_shift == 0) &&
        (parameters->granted_protocol == ObjectHeaderBitsProtocol::None) &&
        (parameters->granted_atomic_operations == 0) &&
        (parameters->granted_memory_order == ObjectHeaderBitsMemoryOrder::None) &&
        (parameters->clear_state == 0) &&
        (parameters->invalid_state == 0) &&
        (parameters->transition_state == 0) &&
        (parameters->published_state == 0);
}
}

HRESULT GCHeapUtilities::ConfigureObjectHeaderBits(IGCHeap* gcHeap)
{
    ObjectHeaderBitsParameters* parameters = gcHeap->GetObjectHeaderBitsParameters();
    if (parameters == nullptr)
    {
        return E_FAIL;
    }

    if (parameters->request_status != ObjectHeaderBitsRequestStatus::NotProcessed)
    {
        parameters->request_status = ObjectHeaderBitsRequestStatus::Unsupported;
        ClearObjectHeaderBitsRuntimeOutputs(parameters);
        return E_FAIL;
    }

    if (!ObjectHeaderBitsRuntimeOutputsAreZero(parameters))
    {
        parameters->request_status = ObjectHeaderBitsRequestStatus::Unsupported;
        ClearObjectHeaderBitsRuntimeOutputs(parameters);
        return E_INVALIDARG;
    }

    switch (parameters->request)
    {
        case ObjectHeaderBitsRequest::Disabled:
            if ((parameters->version != 0) ||
                (parameters->size != 0) ||
                (parameters->requested_bit_count != 0) ||
                (parameters->requested_state_count != 0) ||
                (parameters->requested_protocol != ObjectHeaderBitsProtocol::None) ||
                (parameters->required_atomic_operations != 0) ||
                (parameters->required_memory_order != ObjectHeaderBitsMemoryOrder::None))
            {
                parameters->request_status = ObjectHeaderBitsRequestStatus::Unsupported;
                return E_INVALIDARG;
            }

            parameters->request_status = ObjectHeaderBitsRequestStatus::Accepted;
            return S_OK;

        case ObjectHeaderBitsRequest::Enabled:
            break;

        default:
            parameters->request_status = ObjectHeaderBitsRequestStatus::Unsupported;
            return E_INVALIDARG;
    }

    if ((parameters->version != GC_OBJECT_HEADER_BITS_PARAMETERS_VERSION) ||
        (parameters->size != sizeof(ObjectHeaderBitsParameters)) ||
        (parameters->requested_bit_count == 0) ||
        (parameters->requested_state_count == 0) ||
        (parameters->requested_protocol == ObjectHeaderBitsProtocol::None) ||
        ((parameters->required_atomic_operations & ~ObjectHeaderBitsSupportedAtomicOperations) != 0) ||
        (parameters->required_memory_order != ObjectHeaderBitsMemoryOrder::SequentiallyConsistent))
    {
        parameters->request_status = ObjectHeaderBitsRequestStatus::Unsupported;
        return E_INVALIDARG;
    }

    if ((parameters->requested_bit_count != 2) ||
        (parameters->requested_state_count != 3) ||
        (parameters->requested_protocol != ObjectHeaderBitsProtocol::ClaimAndPublish) ||
        (parameters->required_atomic_operations != ObjectHeaderBitsSupportedAtomicOperations) ||
        (parameters->required_memory_order != ObjectHeaderBitsMemoryOrder::SequentiallyConsistent))
    {
        parameters->request_status = ObjectHeaderBitsRequestStatus::Unsupported;
        return E_NOTIMPL;
    }

#ifndef TARGET_64BIT
    parameters->request_status = ObjectHeaderBitsRequestStatus::Unsupported;
    return E_NOTIMPL;
#else
    static_assert(sizeof(ObjHeader) == sizeof(void*));
    parameters->object_byte_offset = -static_cast<int32_t>(sizeof(ObjHeader));
    parameters->storage_word_size = sizeof(uint32_t);
    parameters->bit_mask = 0x00000003;
    parameters->bit_shift = 0;
    parameters->granted_protocol = ObjectHeaderBitsProtocol::ClaimAndPublish;
    parameters->granted_atomic_operations = ObjectHeaderBitsSupportedAtomicOperations;
    parameters->granted_memory_order = ObjectHeaderBitsMemoryOrder::SequentiallyConsistent;
    parameters->clear_state = 0b00;
    parameters->invalid_state = 0b01;
    parameters->transition_state = 0b10;
    parameters->published_state = 0b11;
    parameters->request_status = ObjectHeaderBitsRequestStatus::Accepted;
    return S_OK;
#endif
}

// Initializes a non-standalone GC. The protocol for initializing a non-standalone GC
// is similar to loading a standalone one, except that the GC_VersionInfo and
// GC_Initialize symbols are linked to directory and thus don't need to be loaded.
//
HRESULT GCHeapUtilities::InitializeDefaultGC()
{
    // we should only call this once on startup. Attempting to load a GC
    // twice is an error.
    assert(g_pGCHeap == nullptr);

    IGCHeap* heap;
    IGCHandleManager* manager;
    g_gc_dac_vars.major_version_number = GC_INTERFACE_MAJOR_VERSION;
    g_gc_dac_vars.minor_version_number = GC_INTERFACE_MINOR_VERSION;
    HRESULT initResult = GC_Initialize(nullptr, &heap, &manager, &g_gc_dac_vars);

    if (initResult == S_OK)
    {
        initResult = ConfigureObjectReferenceEnumeration(heap);
    }

    if (initResult == S_OK)
    {
        initResult = ConfigureObjectHeaderBits(heap);
    }

    if (initResult == S_OK)
    {
        g_pGCHeap = heap;
        g_pGCHandleManager = manager;
        g_gcDacGlobals = &g_gc_dac_vars;
        LOG((LF_GC, LL_INFO100, "GC load successful\n"));
    }
    else
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with HR = 0x%X\n", initResult));
    }

    return initResult;
}

#endif // DACCESS_COMPILE

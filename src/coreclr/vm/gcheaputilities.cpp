// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "configuration.h"
#include "gcheaputilities.h"
#include "appdomain.hpp"
#include "hostinformation.h"

#include "../gc/env/gcenv.ee.h"
#ifdef FEATURE_STANDALONE_GC
#include "../gc/env/gctoeeinterface.standalone.inl"
#endif // FEATURE_STANDALONE_GC

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
uint8_t* g_region_to_generation_table = nullptr;
uint8_t  g_region_shr = 0;
bool g_region_use_bitwise_write_barrier = false;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
uint32_t* g_card_bundle_table = nullptr;
#endif

// This is the global GC heap, maintained by the VM.
GPTR_IMPL(IGCHeap, g_pGCHeap);

GcDacVars g_gc_dac_vars;
GPTR_IMPL(GcDacVars, g_gcDacGlobals);

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

uint8_t* g_write_watch_table = nullptr;
bool g_sw_ww_enabled_for_gc_heap = false;

#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

// Unused - kept for GC data contract c1 compatibility, see datadescriptor/datadescriptor.inc.
GVAL_IMPL_INIT(ee_alloc_context, g_global_alloc_context, {});

thread_local ee_alloc_context::PerThreadRandom ee_alloc_context::t_random = PerThreadRandom();

enum GC_LOAD_STATUS {
    GC_LOAD_STATUS_BEFORE_START,
    GC_LOAD_STATUS_START,
    GC_LOAD_STATUS_DONE_LOAD,
    GC_LOAD_STATUS_GET_VERSIONINFO,
    GC_LOAD_STATUS_CALL_VERSIONINFO,
    GC_LOAD_STATUS_DONE_VERSION_CHECK,
    GC_LOAD_STATUS_GET_INITIALIZE,
    GC_LOAD_STATUS_LOAD_COMPLETE
};

// Load status of the GC. If GC loading fails, the value of this
// global indicates where the failure occurred.
GC_LOAD_STATUS g_gc_load_status = GC_LOAD_STATUS_BEFORE_START;

// The version of the GC that we have loaded.
VersionInfo g_gc_version_info;

// The module that contains the GC.
PTR_VOID g_gc_module_base;

FastGCFunctions GCHeapUtilities::s_fastGCFunctions = {};
void GCHeapUtilities::InitializeFastGCFunctions(IGCHeap* gcHeap)
{
    s_fastGCFunctions = {};
    gcHeap->GetFastGCFunctions(&s_fastGCFunctions);

    // The fields after bulk_move_with_write_barrier exist since interface 6.1. A collector that was built against 6.0
    // does not know them, so they stay zero for it.
    if ((g_gc_version_info.MajorVersion != GC_INTERFACE_MAJOR_VERSION) ||
        (g_gc_version_info.MinorVersion < GC_OLD_VALUE_STORE_HOOKS_INTERFACE_MINOR_VERSION))
    {
        s_fastGCFunctions.pre_store_range = nullptr;
        s_fastGCFunctions.flags = 0;
    }
}

// GC entrypoints for the linked-in GC. These symbols are invoked
// directly if we are not using a standalone GC.
extern "C" void LOCALGC_CALLCONV GC_VersionInfo(/* Out */ VersionInfo* info);
extern "C" HRESULT LOCALGC_CALLCONV GC_Initialize(
    /* In  */ IGCToCLR* clrToGC,
    /* Out */ IGCHeap** gcHeap,
    /* Out */ IGCHandleManager** gcHandleManager,
    /* Out */ GcDacVars* gcDacVars
);

#ifndef DACCESS_COMPILE

PTR_VOID GCHeapUtilities::GetGCModuleBase()
{
    assert(g_gc_module_base);
    return g_gc_module_base;
}

namespace
{
Object** LOCALGC_CALLCONV GetLoaderAllocatorObjectSlotForGC(MethodTable* methodTable)
{
    LIMITED_METHOD_CONTRACT;

    if (!methodTable->Collectible())
    {
        return nullptr;
    }

    return reinterpret_cast<Object**>(methodTable->GetLoaderAllocatorObjectHandle());
}
}

HRESULT GCHeapUtilities::ConfigureObjectReferenceEnumeration(IGCHeap* gcHeap, const VersionInfo& version)
{
    if ((version.MajorVersion != GC_INTERFACE_MAJOR_VERSION) ||
        (version.MinorVersion < GC_OBJECT_REFERENCE_ENUMERATION_INTERFACE_MINOR_VERSION))
    {
        return S_OK;
    }

    ObjectReferenceEnumerationParameters* parameters =
        gcHeap->GetObjectReferenceEnumerationParameters();
    if (parameters == nullptr)
    {
        LogErrorToHost("GC object-reference enumeration descriptor is null.");
        return E_FAIL;
    }

    if (parameters->request_status != ObjectReferenceEnumerationRequestStatus::NotProcessed)
    {
        LogErrorToHost(
            "GC object-reference enumeration descriptor was not initialized to NotProcessed.");
        parameters->request_status = ObjectReferenceEnumerationRequestStatus::Unsupported;
        return E_FAIL;
    }

    if (parameters->get_loader_allocator_object_slot != nullptr)
    {
        LogErrorToHost(
            "GC object-reference enumeration resolver must be initialized to null.");
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
            LogErrorToHost("GC object-reference enumeration request is invalid.");
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

HRESULT GCHeapUtilities::ConfigureObjectHeaderBits(IGCHeap* gcHeap, const VersionInfo& version)
{
    if ((version.MajorVersion != GC_INTERFACE_MAJOR_VERSION) ||
        (version.MinorVersion < GC_OBJECT_HEADER_BITS_INTERFACE_MINOR_VERSION))
    {
        return S_OK;
    }

    ObjectHeaderBitsParameters* parameters = gcHeap->GetObjectHeaderBitsParameters();
    if (parameters == nullptr)
    {
        LogErrorToHost("GC object-header bit descriptor is null.");
        return E_FAIL;
    }

    if (parameters->request_status != ObjectHeaderBitsRequestStatus::NotProcessed)
    {
        LogErrorToHost("GC object-header bit descriptor was not initialized to NotProcessed.");
        parameters->request_status = ObjectHeaderBitsRequestStatus::Unsupported;
        ClearObjectHeaderBitsRuntimeOutputs(parameters);
        return E_FAIL;
    }

    if (!ObjectHeaderBitsRuntimeOutputsAreZero(parameters))
    {
        LogErrorToHost("GC object-header bit descriptor outputs must be initialized to zero.");
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
                LogErrorToHost("Disabled GC object-header bit request has nonzero requirements.");
                parameters->request_status = ObjectHeaderBitsRequestStatus::Unsupported;
                return E_INVALIDARG;
            }

            parameters->request_status = ObjectHeaderBitsRequestStatus::Accepted;
            return S_OK;

        case ObjectHeaderBitsRequest::Enabled:
            break;

        default:
            LogErrorToHost("GC object-header bit request is invalid.");
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
        LogErrorToHost("GC object-header bit requirements are malformed.");
        parameters->request_status = ObjectHeaderBitsRequestStatus::Unsupported;
        return E_INVALIDARG;
    }

    if ((parameters->requested_bit_count != 2) ||
        (parameters->requested_state_count != 3) ||
        (parameters->requested_protocol != ObjectHeaderBitsProtocol::ClaimAndPublish) ||
        (parameters->required_atomic_operations != ObjectHeaderBitsSupportedAtomicOperations) ||
        (parameters->required_memory_order != ObjectHeaderBitsMemoryOrder::SequentiallyConsistent))
    {
        LogErrorToHost("GC object-header bit requirements are unsupported.");
        parameters->request_status = ObjectHeaderBitsRequestStatus::Unsupported;
        return E_NOTIMPL;
    }

#ifndef TARGET_64BIT
    LogErrorToHost("GC object-header bits require a 64-bit object header.");
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

namespace
{

// This block of code contains all of the state necessary to handle incoming
// EtwCallbacks before the GC has been initialized. This is a tricky problem
// because EtwCallbacks can appear at any time, even when we are just about
// finished initializing the GC.
//
// The below lock is taken by the "main" thread (the thread in EEStartup) and
// the "ETW" thread, the one calling EtwCallback. EtwCallback may or may not
// be called on the main thread.
DangerousNonHostedSpinLock g_eventStashLock;

GCEventLevel g_stashedLevel = GCEventLevel_None;
GCEventKeyword g_stashedKeyword = GCEventKeyword_None;
GCEventLevel g_stashedPrivateLevel = GCEventLevel_None;
GCEventKeyword g_stashedPrivateKeyword = GCEventKeyword_None;

BOOL g_gcEventTracingInitialized = FALSE;

// FinalizeLoad is called by the main thread to complete initialization of the GC.
// At this point, the GC has provided us with an IGCHeap instance and we are preparing
// to "publish" it by assigning it to g_pGCHeap.
//
// This function can proceed concurrently with StashKeywordAndLevel below.
HRESULT FinalizeLoad(
    IGCHeap* gcHeap,
    IGCHandleManager* handleMgr,
    PTR_VOID pGcModuleBase,
    const VersionInfo& version)
{
    HRESULT enumerationResult =
        GCHeapUtilities::ConfigureObjectReferenceEnumeration(gcHeap, version);
    if (FAILED(enumerationResult))
    {
        return enumerationResult;
    }

    HRESULT objectHeaderBitsResult =
        GCHeapUtilities::ConfigureObjectHeaderBits(gcHeap, version);
    if (FAILED(objectHeaderBitsResult))
    {
        return objectHeaderBitsResult;
    }

    g_pGCHeap = gcHeap;
    // Heap type and fast functions exist since interface 6.0. The loader has already rejected lower major versions.
    bool supportsFastGCFunctionsAndHeapType =
        g_gc_version_info.MajorVersion >= GC_INTERFACE_MAJOR_VERSION;
    if (supportsFastGCFunctionsAndHeapType)
    {
        g_heap_type = gcHeap->GetGCHeapType();
    }

    {
        DangerousNonHostedSpinLockHolder lockHolder(&g_eventStashLock);

        // Ultimately, g_eventStashLock ensures that no two threads call ControlEvents at any
        // point in time.
        g_pGCHeap->ControlEvents(g_stashedKeyword, g_stashedLevel);
        g_pGCHeap->ControlPrivateEvents(g_stashedPrivateKeyword, g_stashedPrivateLevel);
        g_gcEventTracingInitialized = TRUE;
    }

    g_pGCHandleManager = handleMgr;
    g_gcDacGlobals = &g_gc_dac_vars;
    g_gc_load_status = GC_LOAD_STATUS_LOAD_COMPLETE;
    g_gc_module_base = pGcModuleBase;

    if (supportsFastGCFunctionsAndHeapType)
    {
        GCHeapUtilities::InitializeFastGCFunctions(gcHeap);
    }

    LOG((LF_GC, LL_INFO100, "GC load successful\n"));

    StressLog::AddModule((uint8_t*)pGcModuleBase);
    return S_OK;
}

void StashKeywordAndLevel(bool isPublicProvider, GCEventKeyword keywords, GCEventLevel level)
{
    DangerousNonHostedSpinLockHolder lockHolder(&g_eventStashLock);
    if (!g_gcEventTracingInitialized)
    {
        if (isPublicProvider)
        {
            g_stashedKeyword = keywords;
            g_stashedLevel = level;
        }
        else
        {
            g_stashedPrivateKeyword = keywords;
            g_stashedPrivateLevel = level;
        }
    }
    else
    {
        if (isPublicProvider)
        {
            g_pGCHeap->ControlEvents(keywords, level);
        }
        else
        {
            g_pGCHeap->ControlPrivateEvents(keywords, level);
        }
    }
}

#ifdef FEATURE_STANDALONE_GC
HMODULE LoadStandaloneGc(LPCWSTR libFileName, LPCWSTR libFilePath)
{
    LIMITED_METHOD_CONTRACT;
    HMODULE result = nullptr;

    if (libFilePath)
    {
        return CLRLoadLibrary(libFilePath);
    }

    //
    // This is not a security feature.
    // The libFileName originates either from an environment variable or from the runtimeconfig.json
    // These are trusted locations, and therefore even if it is a relative path, there is no security risk.
    //
    // However, users often don't know the absolute path to their coreclr module, especially on production.
    // Therefore we allow referencing it from an arbitrary location through libFilePath instead. Users, however
    // are warned that they should keep the file in a secure location such that it cannot be tampered.
    //
    if (!ValidateModuleName(libFileName))
    {
        MAKE_UTF8PTR_FROMWIDE(libFileNameUtf8, libFileName);
        LOG((LF_GC, LL_INFO100, "Invalid GC name found %s\n", libFileNameUtf8));
        return nullptr;
    }

    // The APP_CONTEXT_BASE_DIRECTORY is always set by the host. In cases
    // where the runtime is activated as a component, the base directory
    // will be an empty string. If the base directory is an empty string, skip it.
    SString appBase;
    if (HostInformation::GetProperty("APP_CONTEXT_BASE_DIRECTORY", appBase)
        && u16_strlen(appBase.GetUnicode()) != 0)
    {
        PathString libPath = appBase.GetUnicode();
        libPath.Append(libFileName);

        LOG((LF_GC, LL_INFO100, "Loading standalone GC from appBase %s\n", libPath.GetUTF8()));

        LPCWSTR libraryName = libPath.GetUnicode();
        result = CLRLoadLibrary(libraryName);
    }

    if (result == nullptr)
    {
        // Look for the standalone GC module next to the clr binary
        PathString libPath;
        IfFailThrow(GetClrModuleDirectory(libPath));
        libPath.Append(libFileName);

        LOG((LF_GC, LL_INFO100, "Loading standalone GC by coreclr %s\n", libPath.GetUTF8()));

        LPCWSTR libraryName = libPath.GetUnicode();
        result = CLRLoadLibrary(libraryName);
    }

    return result;
}
#endif // FEATURE_STANDALONE_GC

// Loads and initializes a standalone GC, given the path to the GC
// that we should load. Returns S_OK on success and the failed HRESULT
// on failure.
//
// See Documentation/design-docs/standalone-gc-loading.md for details
// on the loading protocol in use here.
HRESULT LoadAndInitializeGC(LPCWSTR standaloneGCName, LPCWSTR standaloneGCPath)
{
    LIMITED_METHOD_CONTRACT;

#ifndef FEATURE_STANDALONE_GC
    LOG((LF_GC, LL_FATALERROR, "EE not built with the ability to load standalone GCs"));
    return E_FAIL;
#else
    HMODULE hMod = LoadStandaloneGc(standaloneGCName, standaloneGCPath);
    if (!hMod)
    {
        HRESULT err = GetLastError();
#ifdef LOGGING
        LPCWSTR standaloneGCNameLogging = standaloneGCName ? standaloneGCName : W("");
        LPCWSTR standaloneGCPathLogging = standaloneGCPath ? standaloneGCPath : W("");
        MAKE_UTF8PTR_FROMWIDE(standaloneGCNameUtf8, standaloneGCNameLogging);
        MAKE_UTF8PTR_FROMWIDE(standaloneGCPathUtf8, standaloneGCPathLogging);
        LOG((LF_GC, LL_FATALERROR, "Load of %s or %s failed\n", standaloneGCNameUtf8, standaloneGCPathUtf8));
#endif // LOGGING
        return __HRESULT_FROM_WIN32(err);
    }

    // a standalone GC dispatches virtually on GCToEEInterface, so we must instantiate
    // a class for the GC to use.
    IGCToCLR* gcToClr = new (nothrow) standalone::GCToEEInterface();
    if (!gcToClr)
    {
        return E_OUTOFMEMORY;
    }

    g_gc_load_status = GC_LOAD_STATUS_DONE_LOAD;
    GC_VersionInfoFunction versionInfo = (GC_VersionInfoFunction)GetProcAddress(hMod, "GC_VersionInfo");
    if (!versionInfo)
    {
        HRESULT err = GetLastError();
        LOG((LF_GC, LL_FATALERROR, "Load of `GC_VersionInfo` from standalone GC failed\n"));
        return __HRESULT_FROM_WIN32(err);
    }

    g_gc_load_status = GC_LOAD_STATUS_GET_VERSIONINFO;
    g_gc_version_info.MajorVersion = EE_INTERFACE_MAJOR_VERSION;
    g_gc_version_info.MinorVersion = 0;
    g_gc_version_info.BuildVersion = 0;
    versionInfo(&g_gc_version_info);
    g_gc_load_status = GC_LOAD_STATUS_CALL_VERSIONINFO;

    if (g_gc_version_info.MajorVersion < GC_INTERFACE_MAJOR_VERSION)
    {
        LOG((LF_GC, LL_FATALERROR, "Loaded GC has incompatible major version number (expected at least %d, got %d)\n",
            GC_INTERFACE_MAJOR_VERSION, g_gc_version_info.MajorVersion));
        return E_FAIL;
    }

    if (g_gc_version_info.MinorVersion < GC_INTERFACE_MINOR_VERSION)
    {
        LOG((LF_GC, LL_INFO100, "Loaded GC has lower minor version number (%d) than EE was compiled against (%d)\n",
            g_gc_version_info.MinorVersion, GC_INTERFACE_MINOR_VERSION));
    }

    LOG((LF_GC, LL_INFO100, "Loaded GC identifying itself with name `%s`\n", g_gc_version_info.Name));
    g_gc_load_status = GC_LOAD_STATUS_DONE_VERSION_CHECK;
    GC_InitializeFunction initFunc = (GC_InitializeFunction)GetProcAddress(hMod, "GC_Initialize");
    if (!initFunc)
    {
        HRESULT err = GetLastError();
        LOG((LF_GC, LL_FATALERROR, "Load of `GC_Initialize` from standalone GC failed\n"));
        return __HRESULT_FROM_WIN32(err);
    }

    g_gc_load_status = GC_LOAD_STATUS_GET_INITIALIZE;
    IGCHeap* heap;
    IGCHandleManager* manager;
    HRESULT initResult = initFunc(gcToClr, &heap, &manager, &g_gc_dac_vars);
    if (initResult == S_OK)
    {
        PTR_VOID pGcModuleBase;

#if TARGET_WINDOWS
        pGcModuleBase = (PTR_VOID)hMod;
#else
        pGcModuleBase = (PTR_VOID)PAL_GetSymbolModuleBase((PVOID)initFunc);
#endif

        initResult = FinalizeLoad(heap, manager, pGcModuleBase, g_gc_version_info);
    }
    else
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with HR = 0x%X\n", initResult));
    }

    return initResult;
#endif // FEATURE_STANDALONE_GC
}

// Initializes a non-standalone GC. The protocol for initializing a non-standalone GC
// is similar to loading a standalone one, except that the GC_VersionInfo and
// GC_Initialize symbols are linked to directory and thus don't need to be loaded.
//
// The major and minor versions are still checked in debug builds - it must be the case
// that the GC and EE agree on a shared version number because they are built from
// the same sources.
HRESULT InitializeDefaultGC()
{
    LIMITED_METHOD_CONTRACT;

    LOG((LF_GC, LL_INFO100, "Standalone GC location not provided, using provided GC\n"));

    g_gc_load_status = GC_LOAD_STATUS_DONE_LOAD;
    GC_VersionInfo(&g_gc_version_info);
    g_gc_load_status = GC_LOAD_STATUS_CALL_VERSIONINFO;

    // the default GC builds with the rest of the EE. By definition, it must have been
    // built with the same interface version.
    assert(g_gc_version_info.MajorVersion == GC_INTERFACE_MAJOR_VERSION);
    assert(g_gc_version_info.MinorVersion == GC_INTERFACE_MINOR_VERSION);
    g_gc_load_status = GC_LOAD_STATUS_DONE_VERSION_CHECK;

    IGCHeap* heap;
    IGCHandleManager* manager;
    HRESULT initResult = GC_Initialize(nullptr, &heap, &manager, &g_gc_dac_vars);
    if (initResult == S_OK)
    {
        initResult = FinalizeLoad(heap, manager, GetClrModuleBase(), g_gc_version_info);
    }
    else
    {
        LOG((LF_GC, LL_FATALERROR, "GC initialization failed with HR = 0x%X\n", initResult));
    }


    return initResult;
}

} // anonymous namespace

// Loads (if necessary) and initializes the GC. If using a standalone GC,
// it loads the library containing it and dynamically loads the GC entry point.
// If using a non-standalone GC, it invokes the GC entry point directly.
HRESULT GCHeapUtilities::LoadAndInitialize()
{
    LIMITED_METHOD_CONTRACT;

    // we should only call this once on startup. Attempting to load a GC
    // twice is an error.
    assert(g_pGCHeap == nullptr);

    // we should not have attempted to load a GC already. Attempting a
    // load after the first load already failed is an error.
    assert(g_gc_load_status == GC_LOAD_STATUS_BEFORE_START);
    g_gc_load_status = GC_LOAD_STATUS_START;

    LPCWSTR standaloneGCName = Configuration::GetKnobStringValue(W("System.GC.Name"), CLRConfig::EXTERNAL_GCName);
    LPCWSTR standaloneGCPath = Configuration::GetKnobStringValue(W("System.GC.Path"), CLRConfig::EXTERNAL_GCPath);
    g_gc_dac_vars.major_version_number = GC_INTERFACE_MAJOR_VERSION;
    g_gc_dac_vars.minor_version_number = GC_INTERFACE_MINOR_VERSION;
    if (!standaloneGCName && !standaloneGCPath)
    {
        return InitializeDefaultGC();
    }
    else
    {
        return LoadAndInitializeGC(standaloneGCName, standaloneGCPath);
    }
}

void GCHeapUtilities::RecordEventStateChange(bool isPublicProvider, GCEventKeyword keywords, GCEventLevel level)
{
    CONTRACTL {
      MODE_ANY;
      NOTHROW;
      GC_NOTRIGGER;
      CAN_TAKE_LOCK;
    } CONTRACTL_END;

    StashKeywordAndLevel(isPublicProvider, keywords, level);
}

#endif // DACCESS_COMPILE

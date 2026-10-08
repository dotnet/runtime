// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ===========================================================================
// File: writebariermanager.cpp
// ===========================================================================

// This contains JITinterface routines for managing which write barrier function
// is currently in use, and patching all related constants.

#include "common.h"
#include "jitinterface.h"
#include "eeconfig.h"
#include "excep.h"
#include "threadsuspend.h"
#include "writebarriermanager.h"
#if !defined(WRITE_BARRIER_VARS_INLINE)
#include "patchedcodeconstants.h"
#endif

#include "perfmap.h"

extern uint8_t* g_ephemeral_low;
extern uint8_t* g_ephemeral_high;
extern uint32_t* g_card_table;
extern uint32_t* g_card_bundle_table;

// Patch Labels for the various write barriers

EXTERN_C void JIT_WriteBarrier_End();
EXTERN_C void JIT_WriteBarrier_PreGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_PreGrow64_End();
EXTERN_C void JIT_WriteBarrier_PostGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_PostGrow64_End();
#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_SVR64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_SVR64_End();
#endif // FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_Byte_Region64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_Byte_Region64_End();
EXTERN_C void JIT_WriteBarrier_Bit_Region64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_Bit_Region64_End();
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_End();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_End();
#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_End();
#endif // FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_End();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64(Object **dst, Object *ref);
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_End();
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP


#if defined(WRITE_BARRIER_VARS_INLINE)

EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_CardBundleTable();
#endif

EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_CardBundleTable();
#endif

#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_SVR64_PatchLabel_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_SVR64_PatchLabel_CardBundleTable();
#endif
#endif // FEATURE_SVR_GC

EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionShrDest();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionShrSrc();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_CardBundleTable();
#endif

EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionShrDest();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionShrSrc();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_CardBundleTable();
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardBundleTable();
#endif

EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardBundleTable();
#endif

#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardBundleTable();
#endif
#endif // FEATURE_SVR_GC

EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionShrDest();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_RegionShrSrc();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_Byte_Region64_Patch_Label_CardBundleTable();
#endif

EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionShrDest();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionShrSrc();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_CardBundleTable();
#endif

#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#else // WRITE_BARRIER_VARS_INLINE

EXTERN_C void JIT_WriteBarrier_Table();
EXTERN_C void JIT_WriteBarrier_Table_End();
EXTERN_C void JIT_WriteBarrier_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_Patch_Label_RegionShr();
EXTERN_C void JIT_WriteBarrier_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_Patch_Label_CardBundleTable();
#endif
#if defined(TARGET_ARM64)
EXTERN_C void JIT_WriteBarrier_Patch_Label_LowestAddress();
EXTERN_C void JIT_WriteBarrier_Patch_Label_HighestAddress();
#if defined(WRITE_BARRIER_CHECK)
EXTERN_C void JIT_WriteBarrier_Patch_Label_GCShadow();
EXTERN_C void JIT_WriteBarrier_Patch_Label_GCShadowEnd();
#endif // WRITE_BARRIER_CHECK
#endif // TARGET_ARM64
#endif // WRITE_BARRIER_VARS_INLINE

#ifndef FEATURE_PORTABLE_HELPERS

extern "C" void STDCALL JIT_PatchedCodeStart();
extern "C" void STDCALL JIT_PatchedCodeLast();
#ifdef TARGET_X86
extern "C" void STDCALL JIT_PatchedWriteBarrierGroup_End();
#else
extern "C" void STDCALL JIT_WriteBarrier_End();
#if defined(TARGET_ARM64) || defined(TARGET_ARM) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
extern "C" void STDCALL JIT_CheckedWriteBarrier_End();
#endif
#endif // TARGET_X86

static void* s_barrierCopy = NULL;
static BYTE* s_barrierOriginal = (BYTE*)JIT_PatchedCodeStart;
static size_t s_barrierCopySize;
static size_t s_customAssignRefSize;
static size_t s_customCheckedAssignRefSize;
// Patchable global-value slots embedded in the custom GC's assign-reference helpers (see
// GCWriteBarrierPatchableValue and GetAssignRefFunctions). Null until InitJITWriteBarrierHelpers
// has run for a GC_HEAP_CUSTOM heap.
static GCWriteBarrierPatchableValue* s_customAssignRefPatchableValues = nullptr;

BYTE* GetWriteBarrierCodeLocation(VOID* barrier)
{
    if (IsWriteBarrierCopyEnabled())
    {
        return (BYTE*)PINSTRToPCODE((TADDR)s_barrierCopy + ((TADDR)barrier - (TADDR)s_barrierOriginal));
    }
    else
    {
        return (BYTE*)barrier;
    }
}

BOOL IsIPInWriteBarrierCodeCopy(PCODE controlPc)
{
    if (IsWriteBarrierCopyEnabled())
    {
        return s_barrierCopy != nullptr &&
            controlPc >= (PCODE)s_barrierCopy && controlPc - (PCODE)s_barrierCopy < s_barrierCopySize;
    }
    else
    {
        return FALSE;
    }
}

PCODE AdjustWriteBarrierIP(PCODE controlPc)
{
    _ASSERTE(IsIPInWriteBarrierCodeCopy(controlPc));

    // Pretend we were executing the barrier function at its original location so that the unwinder can unwind the frame
    return (PCODE)s_barrierOriginal + (controlPc - (PCODE)s_barrierCopy);
}

// Refreshes the patchable global-value slots embedded in the custom GC's assign-reference helpers
// (see GCWriteBarrierPatchableValue) so that they reflect the current values of the globals they
// cache. This must be called after the helpers are first copied (if write barrier copying is
// enabled) and again whenever any of the referenced globals' values change, since the helpers
// never read the globals directly. A no-op until InitJITWriteBarrierHelpers has populated
// s_customAssignRefPatchableValues.
void UpdateCustomWriteBarrierGlobals()
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    for (GCWriteBarrierPatchableValue* patchableValue = s_customAssignRefPatchableValues; patchableValue != nullptr && patchableValue->Slot != nullptr; patchableValue++)
    {
        UINT_PTR* slot = (UINT_PTR*)GetWriteBarrierCodeLocation(patchableValue->Slot);

        // The slot must be 8-byte aligned so that patching it is a single atomic store (the
        // amd64 helpers hand-tune their NOP padding to guarantee this; see writebarrier.asm).
        _ASSERTE(((UINT_PTR)slot % sizeof(UINT_PTR)) == 0);

        UINT_PTR value = *(UINT_PTR*)patchableValue->Source;
        if (*slot != value)
        {
            ExecutableWriterHolder<UINT_PTR> slotWriterHolder(slot, sizeof(UINT_PTR));
            *slotWriterHolder.GetRW() = value;
            FlushInstructionCache(GetCurrentProcess(), slot, sizeof(UINT_PTR));
        }
    }
}

#ifdef TARGET_X86
extern "C" void *JIT_WriteBarrierEAX_Loc;
#elif TARGET_AMD64
extern "C" void *JIT_WriteBarrier_Loc;
#else
extern "C" void *JIT_WriteBarrier_Loc;
void *JIT_WriteBarrier_Loc = 0;
#endif

#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
extern "C" void *JIT_WriteBarrier_Table_Loc;
void *JIT_WriteBarrier_Table_Loc = 0;
#endif // TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64

#else // FEATURE_PORTABLE_HELPERS

BOOL IsIPInWriteBarrierCodeCopy(PCODE controlPc)
{
    return FALSE;
}

PCODE AdjustWriteBarrierIP(PCODE controlPc)
{
    UNREACHABLE();
}

#endif // FEATURE_PORTABLE_HELPERS

#ifndef FEATURE_PORTABLE_HELPERS
template <typename TAction>
static void ReportCopiedWriteBarrier(TAction action, PCODE address, size_t size, const char* name, LPCWSTR nameW)
{
    LIMITED_METHOD_CONTRACT;

    _ASSERTE(IsIPInWriteBarrierCodeCopy(address));
    _ASSERTE(size != 0);
    action(address, size, name, nameW);
}

template <typename TAction>
static void EnumerateCopiedWriteBarriers(TAction action)
{
    LIMITED_METHOD_CONTRACT;

    if (IsWriteBarrierCopyEnabled())
    {
        if (g_heap_type == GC_HEAP_CUSTOM)
        {
            ReportCopiedWriteBarrier(action,
                VolatileLoad(&hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_ASSIGN_REF].pfnHelper),
                s_customAssignRefSize, "WriteBarrier", W("WriteBarrier"));
            ReportCopiedWriteBarrier(action,
                VolatileLoad(&hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_CHECKED_ASSIGN_REF].pfnHelper),
                s_customCheckedAssignRefSize, "CheckedWriteBarrier", W("CheckedWriteBarrier"));
            return;
        }

#ifdef TARGET_X86
        struct WriteBarrierEntry
        {
            PCODE Address;
            const char* Name;
            LPCWSTR NameW;
        };

        // Use the configured helper targets as the source of truth for which register-specific
        // barriers execute from the private copy.
#define X86_WRITE_BARRIER_REGISTER(reg) \
        { VolatileLoad(&hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_ASSIGN_REF_##reg].pfnHelper), "WriteBarrier" #reg, W("WriteBarrier" #reg) },

        WriteBarrierEntry writeBarriers[] =
        {
            ENUM_X86_WRITE_BARRIER_REGISTERS()
        };

#undef X86_WRITE_BARRIER_REGISTER

        // The helper enumeration order is independent of the assembly layout. Sort by address so
        // each barrier can be sized to the beginning of the next barrier.
        for (size_t i = 1; i < ARRAY_SIZE(writeBarriers); i++)
        {
            WriteBarrierEntry current = writeBarriers[i];
            size_t j = i;
            while (j > 0 && writeBarriers[j - 1].Address > current.Address)
            {
                writeBarriers[j] = writeBarriers[j - 1];
                j--;
            }
            writeBarriers[j] = current;
        }

        // The final barrier ends at the explicit end of the patched write-barrier group.
        PCODE writeBarrierGroupEnd = reinterpret_cast<PCODE>(
            GetWriteBarrierCodeLocation((void*)JIT_PatchedWriteBarrierGroup_End));
        for (size_t i = 0; i < ARRAY_SIZE(writeBarriers); i++)
        {
            // Event tracing reports a start address and byte count, so end is exclusive.
            PCODE end = i + 1 < ARRAY_SIZE(writeBarriers) ? writeBarriers[i + 1].Address : writeBarrierGroupEnd;
            _ASSERTE(writeBarriers[i].Address < end);
            ReportCopiedWriteBarrier(
                action,
                writeBarriers[i].Address,
                end - writeBarriers[i].Address,
                writeBarriers[i].Name,
                writeBarriers[i].NameW);
        }
#else
        // The configured helper target identifies the executable copy; the assembly end label
        // provides the exact size without inspecting the copied instructions.
        PCODE writeBarrier = VolatileLoad(&hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_ASSIGN_REF].pfnHelper);
        ReportCopiedWriteBarrier(
            action,
            writeBarrier,
            (BYTE*)JIT_WriteBarrier_End - (BYTE*)JIT_WriteBarrier,
            "WriteBarrier",
            W("WriteBarrier"));

#if defined(TARGET_ARM64) || defined(TARGET_ARM) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        PCODE checkedWriteBarrier = VolatileLoad(&hlpDynamicFuncTable[DYNAMIC_CORINFO_HELP_CHECKED_ASSIGN_REF].pfnHelper);
        // Some targets use a checked helper outside the copied region. Report it only when the
        // configured helper actually points into the copy.
        if (IsIPInWriteBarrierCodeCopy(checkedWriteBarrier))
        {
            ReportCopiedWriteBarrier(
                action,
                checkedWriteBarrier,
                (BYTE*)JIT_CheckedWriteBarrier_End - (BYTE*)JIT_CheckedWriteBarrier,
                "CheckedWriteBarrier",
                W("CheckedWriteBarrier"));
        }
#endif // TARGET_ARM64 || TARGET_ARM || TARGET_LOONGARCH64 || TARGET_RISCV64
#endif // TARGET_X86
    }
}

void ReportCopiedWriteBarriersToPerfMap()
{
    WRAPPER_NO_CONTRACT;

    EnumerateCopiedWriteBarriers([](PCODE address, size_t size, const char* name, LPCWSTR)
    {
        PerfMap::LogStubs("WriteBarrier", name, address, size, PerfMapStubType::Individual);
    });
}

#ifdef FEATURE_EVENT_TRACE
void ReportCopiedWriteBarriersToEventTracing(DWORD eventOptions)
{
    WRAPPER_NO_CONTRACT;

    EnumerateCopiedWriteBarriers([eventOptions](PCODE address, size_t size, const char*, LPCWSTR name)
    {
        _ASSERTE(FitsInU4(size));
        ETW::MethodLog::SendCopiedWriteBarrierEvent(address, static_cast<ULONG>(size), name, eventOptions);
    });
}
#endif // FEATURE_EVENT_TRACE
#endif // !FEATURE_PORTABLE_HELPERS

WriteBarrierManager g_WriteBarrierManager;

WriteBarrierManager::WriteBarrierManager() :
    m_currentWriteBarrier(WRITE_BARRIER_UNINITIALIZED)
{
    LIMITED_METHOD_CONTRACT;
}

PCODE WriteBarrierManager::GetCurrentWriteBarrierCode()
{
    LIMITED_METHOD_CONTRACT;

    switch (m_currentWriteBarrier)
    {
    	case WRITE_BARRIER_CUSTOM:
            _ASSERTE(!"Custom write barrier should not reach this point");
        	return NULL;
        case WRITE_BARRIER_PREGROW64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_PreGrow64);
        case WRITE_BARRIER_POSTGROW64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_SVR64);
#endif // FEATURE_SVR_GC
        case WRITE_BARRIER_BYTE_REGIONS64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_Byte_Region64);
        case WRITE_BARRIER_BIT_REGIONS64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_Bit_Region64);
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_PreGrow64);
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_SVR64);
#endif // FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_Byte_Region64);
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            return GetEEFuncEntryPoint(JIT_WriteBarrier_WriteWatch_Bit_Region64);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        default:
            UNREACHABLE_MSG("unexpected m_currentWriteBarrier!");
    };
}

size_t WriteBarrierManager::GetSpecificWriteBarrierSize(WriteBarrierType writeBarrier)
{
// marked asm functions are those which use the LEAF_END_MARKED macro to end them which
// creates a public Name_End label which can be used to figure out their size without
// having to create unwind info.
#define MARKED_FUNCTION_SIZE(pfn)    (size_t)((LPBYTE)GetEEFuncEntryPoint(pfn##_End) - (LPBYTE)GetEEFuncEntryPoint(pfn))

    switch (writeBarrier)
    {
	    case WRITE_BARRIER_CUSTOM:
            _ASSERTE(!"Custom write barrier should not reach this point");
    	    return 0;
        case WRITE_BARRIER_PREGROW64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_PreGrow64);
        case WRITE_BARRIER_POSTGROW64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_SVR64);
#endif // FEATURE_SVR_GC
        case WRITE_BARRIER_BYTE_REGIONS64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_Byte_Region64);
        case WRITE_BARRIER_BIT_REGIONS64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_Bit_Region64);
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_PreGrow64);
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_SVR64);
#endif // FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_Byte_Region64);
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier_WriteWatch_Bit_Region64);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_BUFFER:
#if defined(WRITE_BARRIER_VARS_INLINE)
            return MARKED_FUNCTION_SIZE(JIT_WriteBarrier);
#else
            return (size_t)((LPBYTE)GetEEFuncEntryPoint(JIT_WriteBarrier_Table_End) - (LPBYTE)GetEEFuncEntryPoint(JIT_WriteBarrier));
#endif
        default:
            UNREACHABLE_MSG("unexpected m_currentWriteBarrier!");
    };
#undef MARKED_FUNCTION_SIZE
}

size_t WriteBarrierManager::GetCurrentWriteBarrierSize()
{
    return GetSpecificWriteBarrierSize(m_currentWriteBarrier);
}


int WriteBarrierManager::ChangeWriteBarrierTo(WriteBarrierType newWriteBarrier, bool isRuntimeSuspended)
{
    GCX_MAYBE_COOP_NO_THREAD_BROKEN((!isRuntimeSuspended && GetThreadNULLOk() != NULL));
    int stompWBCompleteActions = SWB_PASS;
    if (!isRuntimeSuspended && m_currentWriteBarrier != WRITE_BARRIER_UNINITIALIZED)
    {
        ThreadSuspend::SuspendEE(ThreadSuspend::SUSPEND_FOR_GC_PREP);
        stompWBCompleteActions |= SWB_EE_RESTART;
    }

	if (g_heap_type != GC_HEAP_CUSTOM)
	{
    _ASSERTE(m_currentWriteBarrier != newWriteBarrier);
    m_currentWriteBarrier = newWriteBarrier;

    // the memcpy must come before the switch statement because the asserts inside the switch
    // are actually looking into the JIT_WriteBarrier buffer
    {
        ExecutableWriterHolder<void> writeBarrierWriterHolder(GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier), GetCurrentWriteBarrierSize());
        memcpy(writeBarrierWriterHolder.GetRW(), (LPVOID)GetCurrentWriteBarrierCode(), GetCurrentWriteBarrierSize());
        stompWBCompleteActions |= SWB_ICACHE_FLUSH;
    }
    }
    else
    {
        // TODO: is this method ever called for custom GC?
        _ASSERTE(m_currentWriteBarrier == newWriteBarrier);
    }

#if defined(WRITE_BARRIER_VARS_INLINE)
    UpdatePatchLocations(newWriteBarrier);
#endif

    stompWBCompleteActions |= UpdateEphemeralBounds(true);
    stompWBCompleteActions |= UpdateWriteWatchAndCardTableLocations(true, false);

    return stompWBCompleteActions;
}

void WriteBarrierManager::Initialize()
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    // Ensure that the generic JIT_WriteBarrier function buffer is large enough to hold any of the more specific
    // write barrier implementations.
    size_t cbWriteBarrierBuffer = GetSpecificWriteBarrierSize(WRITE_BARRIER_BUFFER);

    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_PREGROW64));
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_POSTGROW64));
#ifdef FEATURE_SVR_GC
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_SVR64));
#endif // FEATURE_SVR_GC
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_BYTE_REGIONS64));
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_BIT_REGIONS64));
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_PREGROW64));
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_POSTGROW64));
#ifdef FEATURE_SVR_GC
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_SVR64));
#endif // FEATURE_SVR_GC
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64));
    _ASSERTE_ALL_BUILDS(cbWriteBarrierBuffer >= GetSpecificWriteBarrierSize(WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64));
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP


#if !defined(WRITE_BARRIER_VARS_INLINE)

    #define CALC_TABLE_LOCATION(var, offset) \
        assert(JIT_WriteBarrier_Offset_##offset == (PBYTE)JIT_WriteBarrier_Patch_Label_##offset - (PBYTE)JIT_WriteBarrier); \
        var = ((PBYTE)GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier) + JIT_WriteBarrier_Offset_##offset);

    CALC_TABLE_LOCATION(m_pWriteWatchTableImmediate, WriteWatchTable);
    CALC_TABLE_LOCATION(m_pRegionToGenTableImmediate, RegionToGeneration);
    CALC_TABLE_LOCATION(m_pRegionShrDest, RegionShr);
    CALC_TABLE_LOCATION(m_pLowerBoundImmediate, Lower);
    CALC_TABLE_LOCATION(m_pUpperBoundImmediate, Upper);
    CALC_TABLE_LOCATION(m_pCardTableImmediate, CardTable);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    CALC_TABLE_LOCATION(m_pCardBundleTableImmediate, CardBundleTable);
#endif

#if defined(TARGET_ARM64)
    CALC_TABLE_LOCATION(m_lowestAddress, LowestAddress);
    CALC_TABLE_LOCATION(m_highestAddress, HighestAddress);
#if defined(WRITE_BARRIER_CHECK)
    CALC_TABLE_LOCATION(m_pGCShadow, GCShadow);
    CALC_TABLE_LOCATION(m_pGCShadowEnd, GCShadowEnd);
#endif // WRITE_BARRIER_CHECK
#endif // TARGET_AMD64

#endif // !WRITE_BARRIER_VARS_INLINE

#if !defined(CODECOVERAGE) && defined(WRITE_BARRIER_VARS_INLINE)
    Validate();
#endif
}

template <typename T> int updateVariable(PBYTE loc, T value)
{
    if (*(T*)loc != value)
    {
        ExecutableWriterHolder<T> varWriterHolder((T*)loc, sizeof(T));
        *varWriterHolder.GetRW() = value;
        return SWB_ICACHE_FLUSH;
    }
    return SWB_PASS;
}

bool WriteBarrierManager::NeedDifferentWriteBarrier(bool bReqUpperBoundsCheck, bool bUseBitwiseWriteBarrier, WriteBarrierType* pNewWriteBarrierType)
{
    // Init code for the JIT_WriteBarrier assembly routine.  Since it will be bashed everytime the GC Heap
    // changes size, we want to do most of the work just once.
    //
    // The actual JIT_WriteBarrier routine will only be called in free builds, but we keep this code (that
    // modifies it) around in debug builds to check that it works (with assertions).


    WriteBarrierType writeBarrierType = m_currentWriteBarrier;

    for(;;)
    {
        switch (writeBarrierType)
        {
        case WRITE_BARRIER_UNINITIALIZED:
			if (g_heap_type == GC_HEAP_CUSTOM)
			{
				// Satori TODO: is this necessary?
				writeBarrierType = WRITE_BARRIER_CUSTOM;
                break;
			}
#ifdef _DEBUG
            // The default slow write barrier has some good asserts
            if ((g_pConfig->GetHeapVerifyLevel() & EEConfig::HEAPVERIFY_BARRIERCHECK)) {
                break;
            }
#endif
            if (g_region_shr != 0)
            {
                writeBarrierType = bUseBitwiseWriteBarrier ? WRITE_BARRIER_BIT_REGIONS64: WRITE_BARRIER_BYTE_REGIONS64;
            }
            else
            {
#ifdef FEATURE_SVR_GC
                writeBarrierType = GCHeapUtilities::IsServerHeap() ? WRITE_BARRIER_SVR64 : WRITE_BARRIER_PREGROW64;
#else
                writeBarrierType = WRITE_BARRIER_PREGROW64;
#endif // FEATURE_SVR_GC
            }
            continue;

        case WRITE_BARRIER_PREGROW64:
            if (bReqUpperBoundsCheck)
            {
                writeBarrierType = WRITE_BARRIER_POSTGROW64;
            }
            break;

        case WRITE_BARRIER_POSTGROW64:
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            break;
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_BYTE_REGIONS64:
        case WRITE_BARRIER_BIT_REGIONS64:
            break;

        case WRITE_BARRIER_CUSTOM:
            break;

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            if (bReqUpperBoundsCheck)
            {
                writeBarrierType = WRITE_BARRIER_WRITE_WATCH_POSTGROW64;
            }
            break;

        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            break;
#endif // FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            break;
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        default:
            UNREACHABLE_MSG("unexpected write barrier type!");
        }
        break;
    }

    *pNewWriteBarrierType = writeBarrierType;
    return m_currentWriteBarrier != writeBarrierType;
}

int WriteBarrierManager::UpdateEphemeralBounds(bool isRuntimeSuspended)
{
    WriteBarrierType newType;
    if (NeedDifferentWriteBarrier(false, g_region_use_bitwise_write_barrier, &newType))
    {
        return ChangeWriteBarrierTo(newType, isRuntimeSuspended);
    }

    int stompWBCompleteActions = SWB_PASS;

#ifdef _DEBUG
    // Using debug-only write barrier?
    if (m_currentWriteBarrier == WRITE_BARRIER_UNINITIALIZED)
        return stompWBCompleteActions;
#endif

#if defined(WRITE_BARRIER_VARS_INLINE)

    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_POSTGROW64:
        case WRITE_BARRIER_BYTE_REGIONS64:
        case WRITE_BARRIER_BIT_REGIONS64:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
            stompWBCompleteActions |= updateVariable<UINT64>(m_pUpperBoundImmediate, (size_t)g_ephemeral_high);
        FALLTHROUGH;

        case WRITE_BARRIER_PREGROW64:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
            stompWBCompleteActions |= updateVariable<UINT64>(m_pLowerBoundImmediate, (size_t)g_ephemeral_low);
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
            break;
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_CUSTOM:
        {
            // Satori currently only cares about
            // - card_bundle_table  (page index, to get heap bounds)
            // - card_table         (page map, to get to cards)
            // - sw_ww_table        (whether GC is running concurrently)
            break;
        }

        default:
            UNREACHABLE_MSG("unexpected m_currentWriteBarrier in UpdateEphemeralBounds");
    }

#else

    stompWBCompleteActions |= updateVariable<UINT64>(m_pUpperBoundImmediate, (size_t)g_ephemeral_high);
    stompWBCompleteActions |= updateVariable<UINT64>(m_pLowerBoundImmediate, (size_t)g_ephemeral_low);
#endif //WRITE_BARRIER_VARS_INLINE


#if defined(TARGET_ARM64)
    stompWBCompleteActions |= updateVariable<UINT64>(m_lowestAddress, (size_t)g_lowest_address);
    stompWBCompleteActions |= updateVariable<UINT64>(m_highestAddress, (size_t)g_highest_address);
#if defined(WRITE_BARRIER_CHECK)
    stompWBCompleteActions |= updateVariable<UINT64>(m_pGCShadow, (size_t)g_GCShadow);
    stompWBCompleteActions |= updateVariable<UINT64>(m_pGCShadowEnd, (size_t)g_GCShadowEnd);
#endif // WRITE_BARRIER_CHECK
#endif // TARGET_AMD64

    return stompWBCompleteActions;
}

int WriteBarrierManager::UpdateWriteWatchAndCardTableLocations(bool isRuntimeSuspended, bool bReqUpperBoundsCheck)
{
    // If we are told that we require an upper bounds check (GC did some heap reshuffling),
    // we need to switch to the WriteBarrier_PostGrow function for good.

	if (g_heap_type == GC_HEAP_CUSTOM)
	{
#if defined(WRITE_BARRIER_VARS_INLINE)
	    // as of now satori does not patch barriers inline, no need to go further.
    	return SWB_PASS;
#endif
	}
    WriteBarrierType newType;
    if (NeedDifferentWriteBarrier(bReqUpperBoundsCheck, g_region_use_bitwise_write_barrier, &newType))
    {
        return ChangeWriteBarrierTo(newType, isRuntimeSuspended);
    }

    int stompWBCompleteActions = SWB_PASS;

#ifdef _DEBUG
    // Using debug-only write barrier?
    if (m_currentWriteBarrier == WRITE_BARRIER_UNINITIALIZED)
        return stompWBCompleteActions;
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
#if defined(WRITE_BARRIER_VARS_INLINE)
    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
#endif // FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            stompWBCompleteActions |= updateVariable<UINT64>(m_pWriteWatchTableImmediate, (size_t)g_write_watch_table);
            break;

        default:
            break;
    }
#else
    stompWBCompleteActions |= updateVariable<UINT64>(m_pWriteWatchTableImmediate, (size_t)g_write_watch_table);
#endif
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP


#if defined(WRITE_BARRIER_VARS_INLINE)
    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_BYTE_REGIONS64:
        case WRITE_BARRIER_BIT_REGIONS64:
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            stompWBCompleteActions |= updateVariable<UINT64>(m_pRegionToGenTableImmediate, (size_t)g_region_to_generation_table);
            stompWBCompleteActions |= updateVariable<UINT8>(m_pRegionShrDest, (size_t)g_region_shr);
            stompWBCompleteActions |= updateVariable<UINT8>(m_pRegionShrSrc, (size_t)g_region_shr);
            break;

        default:
            break;
    }
#else
    stompWBCompleteActions |= updateVariable<UINT64>(m_pRegionToGenTableImmediate, (size_t)g_region_to_generation_table);
    stompWBCompleteActions |= updateVariable<UINT8>(m_pRegionShrDest, g_region_shr);
#endif //WRITE_BARRIER_VARS_INLINE

    stompWBCompleteActions |= updateVariable<UINT64>(m_pCardTableImmediate, (size_t)g_card_table);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    stompWBCompleteActions |= updateVariable<UINT64>(m_pCardBundleTableImmediate, (size_t)g_card_bundle_table);
#endif

#if defined(TARGET_ARM64)
    stompWBCompleteActions |= updateVariable<UINT64>(m_lowestAddress, (size_t)g_lowest_address);
    stompWBCompleteActions |= updateVariable<UINT64>(m_highestAddress, (size_t)g_highest_address);
#if defined(WRITE_BARRIER_CHECK)
    stompWBCompleteActions |= updateVariable<UINT64>(m_pGCShadow, (size_t)g_GCShadow);
    stompWBCompleteActions |= updateVariable<UINT64>(m_pGCShadowEnd, (size_t)g_GCShadowEnd);
#endif // WRITE_BARRIER_CHECK
#endif // TARGET_AMD64

    return stompWBCompleteActions;
}

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
int WriteBarrierManager::SwitchToWriteWatchBarrier(bool isRuntimeSuspended)
{
    WriteBarrierType newWriteBarrierType;
    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_UNINITIALIZED:
            // Using the debug-only write barrier
            return SWB_PASS;

        case WRITE_BARRIER_PREGROW64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_PREGROW64;
            break;

        case WRITE_BARRIER_POSTGROW64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_POSTGROW64;
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_SVR64;
            break;
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_BYTE_REGIONS64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64;
            break;

        case WRITE_BARRIER_BIT_REGIONS64:
            newWriteBarrierType = WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64;
            break;

        case WRITE_BARRIER_CUSTOM:
            // same barrier, but update variables
            newWriteBarrierType = m_currentWriteBarrier;
            break;

        default:
            UNREACHABLE();
    }

    return ChangeWriteBarrierTo(newWriteBarrierType, isRuntimeSuspended);
}

int WriteBarrierManager::SwitchToNonWriteWatchBarrier(bool isRuntimeSuspended)
{
    WriteBarrierType newWriteBarrierType;
    switch (m_currentWriteBarrier)
    {
        case WRITE_BARRIER_UNINITIALIZED:
            // Using the debug-only write barrier
            return SWB_PASS;

        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            newWriteBarrierType = WRITE_BARRIER_PREGROW64;
            break;

        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            newWriteBarrierType = WRITE_BARRIER_POSTGROW64;
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            newWriteBarrierType = WRITE_BARRIER_SVR64;
            break;
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
            newWriteBarrierType = WRITE_BARRIER_BYTE_REGIONS64;
            break;

        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            newWriteBarrierType = WRITE_BARRIER_BIT_REGIONS64;
            break;

        case WRITE_BARRIER_CUSTOM:
            // same barrier, but update variables
            newWriteBarrierType = m_currentWriteBarrier;
            break;

        default:
            UNREACHABLE();
    }

    return ChangeWriteBarrierTo(newWriteBarrierType, isRuntimeSuspended);
}
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP


#if defined(WRITE_BARRIER_VARS_INLINE)


// Use this somewhat hokey macro to concatenate the function start with the patch
// label. This allows the code below to look relatively nice, but relies on the
// naming convention which we have established for these helpers.
#define CALC_PATCH_LOCATION(func,label,offset)      CalculatePatchLocation((PVOID)func, (PVOID)func##_##label, offset)

PBYTE WriteBarrierManager::CalculatePatchLocation(LPVOID base, LPVOID label, int inlineOffset)
{
    // the label should always come after or at the entrypoint for this funtion
    _ASSERTE_ALL_BUILDS((LPBYTE)label >= (LPBYTE)base);

    BYTE* patchBase = GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier);
    return (patchBase + ((LPBYTE)GetEEFuncEntryPoint(label) - (LPBYTE)GetEEFuncEntryPoint(base))) + inlineOffset;
}

// Deactivate alignment validation for code coverage builds
// because the instrumentation tool will not preserve alignment
// constraints and we will fail.
#if !defined(CODECOVERAGE)

void WriteBarrierManager::Validate()
{
    CONTRACTL
    {
        MODE_ANY;
        GC_NOTRIGGER;
        NOTHROW;
    }
    CONTRACTL_END;

    // we have an invariant that the addresses of all the values that we update in our write barrier
    // helpers must be naturally aligned, this is so that the update can happen atomically since there
    // are places where these values are updated while the EE is running
    // NOTE: we can't call this from the ctor since our infrastructure isn't ready for assert dialogs

    PBYTE pLowerBoundImmediate, pUpperBoundImmediate, pCardTableImmediate;

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    PBYTE pCardBundleTableImmediate;
#endif

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_Lower, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardTable, 2);

    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Lower, 2);
    pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Upper, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

#ifdef FEATURE_SVR_GC
    pCardTableImmediate        = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#endif // FEATURE_SVR_GC

    PBYTE pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_RegionToGeneration, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pRegionToGenTableImmediate) & 0x7) == 0);

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_Lower, 2);
    pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_Upper, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

    pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_RegionToGeneration, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pRegionToGenTableImmediate) & 0x7) == 0);

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_Lower, 2);
    pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_Upper, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    PBYTE pWriteWatchTableImmediate;

    pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_WriteWatchTable, 2);
    pLowerBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_Lower, 2);
    pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardTable, 2);

    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pWriteWatchTableImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

    pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_WriteWatchTable, 2);
    pLowerBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_Lower, 2);
    pUpperBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_Upper, 2);
    pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_CardTable, 2);

    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pWriteWatchTableImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

#ifdef FEATURE_SVR_GC
    pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_WriteWatchTable, 2);
    pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pWriteWatchTableImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#endif // FEATURE_SVR_GC

    pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_RegionToGeneration, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pRegionToGenTableImmediate) & 0x7) == 0);

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_Lower, 2);
    pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_Upper, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

    pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_RegionToGeneration, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pRegionToGenTableImmediate) & 0x7) == 0);

    pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_Lower, 2);
    pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_Upper, 2);
    pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_CardTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pLowerBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pUpperBoundImmediate) & 0x7) == 0);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardTableImmediate) & 0x7) == 0);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_CardBundleTable, 2);
    _ASSERTE_ALL_BUILDS((reinterpret_cast<UINT64>(pCardBundleTableImmediate) & 0x7) == 0);
#endif

#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
}

#endif // CODECOVERAGE

void WriteBarrierManager::UpdatePatchLocations(WriteBarrierType newWriteBarrier)
{
    switch (newWriteBarrier)
    {
        case WRITE_BARRIER_PREGROW64:
        {
            m_pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_Lower, 2);
            m_pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }

        case WRITE_BARRIER_POSTGROW64:
        {
            m_pLowerBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate      = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Upper, 2);
            m_pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
        {
            m_pCardTableImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_BYTE_REGIONS64:
            m_pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_RegionToGeneration, 2);
            m_pRegionShrDest             = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_RegionShrDest, 3);
            m_pRegionShrSrc              = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_RegionShrSrc, 3);
            m_pLowerBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_Upper, 2);
            m_pCardTableImmediate        = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pRegionToGenTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrDest);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrSrc);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Byte_Region64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;

        case WRITE_BARRIER_BIT_REGIONS64:
            m_pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_RegionToGeneration, 2);
            m_pRegionShrDest             = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_RegionShrDest, 3);
            m_pRegionShrSrc              = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_RegionShrSrc, 3);
            m_pLowerBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_Upper, 2);
            m_pCardTableImmediate        = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pRegionToGenTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrDest);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrSrc);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_Bit_Region64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
        {
            m_pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_WriteWatchTable, 2);
            m_pLowerBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_Lower, 2);
            m_pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }

        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
        {
            m_pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_WriteWatchTable, 2);
            m_pLowerBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_Upper, 2);
            m_pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
        {
            m_pWriteWatchTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_WriteWatchTable, 2);
            m_pCardTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;
        }
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
            m_pWriteWatchTableImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_WriteWatchTable, 2);
            m_pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_RegionToGeneration, 2);
            m_pRegionShrDest             = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_RegionShrDest, 3);
            m_pRegionShrSrc              = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_RegionShrSrc, 3);
            m_pLowerBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_Upper, 2);
            m_pCardTableImmediate        = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pRegionToGenTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrDest);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrSrc);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Byte_Region64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;

        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            m_pWriteWatchTableImmediate  = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_WriteWatchTable, 2);
            m_pRegionToGenTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_RegionToGeneration, 2);
            m_pRegionShrDest             = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_RegionShrDest, 3);
            m_pRegionShrSrc              = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_RegionShrSrc, 3);
            m_pLowerBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_Lower, 2);
            m_pUpperBoundImmediate       = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_Upper, 2);
            m_pCardTableImmediate        = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_CardTable, 2);

            // Make sure that we will be bashing the right places (immediates should be hardcoded to 0x0f0f0f0f0f0f0f0f0).
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pWriteWatchTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pRegionToGenTableImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pLowerBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pUpperBoundImmediate);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardTableImmediate);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrDest);
            _ASSERTE_ALL_BUILDS(              0x16 == *(UINT8 *)m_pRegionShrSrc);

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            m_pCardBundleTableImmediate = CALC_PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_Bit_Region64, Patch_Label_CardBundleTable, 2);
            _ASSERTE_ALL_BUILDS(0xf0f0f0f0f0f0f0f0 == *(UINT64*)m_pCardBundleTableImmediate);
#endif
            break;

        case WRITE_BARRIER_CUSTOM:
            // noop
            break;

#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        default:
            UNREACHABLE_MSG("unexpected write barrier type!");
    }
}

#endif // WRITE_BARRIER_VARS_INLINE


// This function bashes the super fast version of the JIT_WriteBarrier
// helper.  It should be called by the GC whenever the ephermeral region
// bounds get changed, but still remain on the top of the GC Heap.
int StompWriteBarrierEphemeral(bool isRuntimeSuspended)
{
    WRAPPER_NO_CONTRACT;

    if (IsWriteBarrierCopyEnabled() && g_heap_type != GC_HEAP_CUSTOM)
        return g_WriteBarrierManager.UpdateEphemeralBounds(isRuntimeSuspended);
    else
        return SWB_PASS;
}

// This function bashes the super fast versions of the JIT_WriteBarrier
// helpers.  It should be called by the GC whenever the ephermeral region gets moved
// from being at the top of the GC Heap, and/or when the cards table gets moved.
int StompWriteBarrierResize(bool isRuntimeSuspended, bool bReqUpperBoundsCheck)
{
    WRAPPER_NO_CONTRACT;

    if (IsWriteBarrierCopyEnabled() && g_heap_type != GC_HEAP_CUSTOM)
        return g_WriteBarrierManager.UpdateWriteWatchAndCardTableLocations(isRuntimeSuspended, bReqUpperBoundsCheck);
    else
        return SWB_PASS;
}

void FlushWriteBarrierInstructionCache()
{
    if (IsWriteBarrierCopyEnabled() && g_heap_type != GC_HEAP_CUSTOM)
        FlushInstructionCache(GetCurrentProcess(), GetWriteBarrierCodeLocation((PVOID)JIT_WriteBarrier), g_WriteBarrierManager.GetCurrentWriteBarrierSize());
}

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

int SwitchToWriteWatchBarrier(bool isRuntimeSuspended)
{
    WRAPPER_NO_CONTRACT;

    if (IsWriteBarrierCopyEnabled() && g_heap_type != GC_HEAP_CUSTOM)
        return g_WriteBarrierManager.SwitchToWriteWatchBarrier(isRuntimeSuspended);
    else
        return SWB_PASS;
}

int SwitchToNonWriteWatchBarrier(bool isRuntimeSuspended)
{
    WRAPPER_NO_CONTRACT;

    if (IsWriteBarrierCopyEnabled() && g_heap_type != GC_HEAP_CUSTOM)
        return g_WriteBarrierManager.SwitchToNonWriteWatchBarrier(isRuntimeSuspended);
    else
        return SWB_PASS;
}
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

void InitJITWriteBarrierHelpers()
{
    STANDARD_VM_CONTRACT;

#ifndef FEATURE_PORTABLE_HELPERS
    void* assignRef = nullptr;
    void* assignRefChecked = nullptr;
    GCWriteBarrierPatchableValue* assignRefPatchableValues = nullptr;
    if (g_heap_type == GC_HEAP_CUSTOM)
    {
        g_pGCHeap->GetAssignRefFunctions(&assignRef, &s_customAssignRefSize,
            &assignRefChecked, &s_customCheckedAssignRefSize, &g_customWriteBarrierAVLocations, &assignRefPatchableValues);
    }

    // All patched helpers should fit into one page.
    // If you hit this assert on retail build, there is most likely problem with BBT script.
    _ASSERTE_ALL_BUILDS((BYTE*)JIT_PatchedCodeLast - (BYTE*)JIT_PatchedCodeStart > (ptrdiff_t)0);
    _ASSERTE_ALL_BUILDS((BYTE*)JIT_PatchedCodeLast - (BYTE*)JIT_PatchedCodeStart < (ptrdiff_t)minipal_getpagesize());

    if (IsWriteBarrierCopyEnabled())
    {
        s_barrierCopySize = (BYTE*)JIT_PatchedCodeLast - (BYTE*)JIT_PatchedCodeStart;
        if (g_heap_type == GC_HEAP_CUSTOM)
        {
            // Preserve the relative placement of both entries, including branches between them.
            size_t allocationSize = g_SystemInfo.dwAllocationGranularity;
            uintptr_t start = min((uintptr_t)assignRef, (uintptr_t)assignRefChecked);
            size_t assignRefOffset = (uintptr_t)assignRef - start;
            size_t checkedOffset = (uintptr_t)assignRefChecked - start;
            if (assignRef == nullptr || assignRefChecked == nullptr ||
                s_customAssignRefSize == 0 || s_customCheckedAssignRefSize == 0 ||
                assignRefOffset >= allocationSize || checkedOffset >= allocationSize ||
                s_customAssignRefSize > allocationSize - assignRefOffset ||
                s_customCheckedAssignRefSize > allocationSize - checkedOffset)
            {
                COMPlusThrow(kInvalidOperationException);
            }

            s_barrierOriginal = (BYTE*)start;
            s_barrierCopySize = max(assignRefOffset + s_customAssignRefSize,
                checkedOffset + s_customCheckedAssignRefSize);
        }

        s_barrierCopy = ExecutableAllocator::Instance()->Reserve(g_SystemInfo.dwAllocationGranularity);
        if (s_barrierCopy == nullptr)
        {
            COMPlusThrowOM();
        }
        if (!ExecutableAllocator::Instance()->Commit(s_barrierCopy, g_SystemInfo.dwAllocationGranularity, true))
        {
            COMPlusThrowOM();
        }

        {
            ExecutableWriterHolder<void> barrierWriterHolder(s_barrierCopy, s_barrierCopySize);
            memcpy(barrierWriterHolder.GetRW(), s_barrierOriginal, s_barrierCopySize);
        }
        FlushInstructionCache(GetCurrentProcess(), s_barrierCopy, s_barrierCopySize);
    }

    if (g_heap_type == GC_HEAP_CUSTOM)
    {
        // Populate the patchable global-value slots embedded in the helpers (see
        // GCWriteBarrierPatchableValue) with the globals' current values. Custom barriers are
        // always copied; GetWriteBarrierCodeLocation resolves each slot into the allocated copy.
        s_customAssignRefPatchableValues = assignRefPatchableValues;
        UpdateCustomWriteBarrierGlobals();

        assignRef = GetWriteBarrierCodeLocation(assignRef);
        assignRefChecked = GetWriteBarrierCodeLocation(assignRefChecked);
#ifndef TARGET_X86
        JIT_WriteBarrier_Loc = assignRef;
#endif
        SetJitHelperFunction(CORINFO_HELP_ASSIGN_REF, assignRef);
        SetJitHelperFunction(CORINFO_HELP_CHECKED_ASSIGN_REF, assignRefChecked);
        SetAuxiliarySymbol(assignRef, "JIT_WriteBarrier");
        SetAuxiliarySymbol(assignRefChecked, "JIT_CheckedWriteBarrier");
        return;
    }

    // Architectures with an inline data table cache addresses within the allocated copy.
    g_WriteBarrierManager.Initialize();

    if (IsWriteBarrierCopyEnabled())
    {
        // Store the JIT_WriteBarrier copy location to a global variable so that helpers
        // can jump to it.
#ifdef TARGET_X86
        JIT_WriteBarrierEAX_Loc = GetWriteBarrierCodeLocation((void*)JIT_WriteBarrierEAX);

#define X86_WRITE_BARRIER_REGISTER(reg) \
    SetJitHelperFunction(CORINFO_HELP_ASSIGN_REF_##reg, GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier##reg)); \
    SetAuxiliarySymbol(GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier##reg), "JIT_WriteBarrier" #reg);

        ENUM_X86_WRITE_BARRIER_REGISTERS()

#undef X86_WRITE_BARRIER_REGISTER

#else // TARGET_X86
        JIT_WriteBarrier_Loc = GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier);
#endif // TARGET_X86
        SetJitHelperFunction(CORINFO_HELP_ASSIGN_REF, GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier));
        SetAuxiliarySymbol(GetWriteBarrierCodeLocation((void*)JIT_WriteBarrier), "JIT_WriteBarrier");

#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        // Store the JIT_WriteBarrier_Table copy location to a global variable so that it can be updated.
        JIT_WriteBarrier_Table_Loc = GetWriteBarrierCodeLocation((void*)&JIT_WriteBarrier_Table);
#endif // TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64

#if defined(TARGET_ARM64) || defined(TARGET_ARM) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        SetJitHelperFunction(CORINFO_HELP_CHECKED_ASSIGN_REF, GetWriteBarrierCodeLocation((void*)JIT_CheckedWriteBarrier));
        SetAuxiliarySymbol(GetWriteBarrierCodeLocation((void*)JIT_CheckedWriteBarrier), "JIT_CheckedWriteBarrier");
#endif // TARGET_ARM64 || TARGET_ARM || TARGET_LOONGARCH64 || TARGET_RISCV64

#if defined(TARGET_AMD64)
        // On AMD64 the Checked variant of the helper jumps through an indirection
        // to the patched barrier, but is not part of the patched set of helpers.
        SetJitHelperFunction(CORINFO_HELP_CHECKED_ASSIGN_REF, (void*)JIT_CheckedWriteBarrier);
#endif // TARGET_AMD64

    }
    else
    {
#ifdef TARGET_X86
        JIT_WriteBarrierEAX_Loc = (void*)RhpAssignRefEAX;
#else
        JIT_WriteBarrier_Loc = (void*)RhpAssignRef;
#endif
#if defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        // Store the JIT_WriteBarrier_Table copy location to a global variable so that it can be updated.
        JIT_WriteBarrier_Table_Loc = NULL;
#endif // TARGET_ARM64 || TARGET_LOONGARCH64 || TARGET_RISCV64
    }
#else
    g_WriteBarrierManager.Initialize();
#endif // !FEATURE_PORTABLE_HELPERS

}

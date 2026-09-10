// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "env/common.h"
#include "env/gcenv.h"
#include "env/gcenv.ee.h"
#include "writebarrierconfig.h"
#if defined(TARGET_ARM64)
#include "patchedcodeconstants.h"
#endif // TARGET_ARM64
#include "writebarrierservices.h"

#ifndef EXTERN_C
#define EXTERN_C extern "C"
#endif

#if defined(TARGET_X86)
#include <minipal/cpuid.h>
#ifndef STDCALL
#ifdef _MSC_VER
#define STDCALL __stdcall
#else
#define STDCALL __attribute__((stdcall))
#endif // _MSC_VER
#endif
#ifndef FASTCALL
#define FASTCALL __fastcall
#endif
#endif // TARGET_X86

#ifdef GC_WRITE_BARRIER_STANDALONE
#include "gcenv.ee.standalone.inl"
#endif // GC_WRITE_BARRIER_STANDALONE

#ifdef TARGET_ARM
#define THUMB_CODE 1
#define GetEEFuncEntryPoint(pfn) (reinterpret_cast<uintptr_t>(pfn) | THUMB_CODE)
#else
#define GetEEFuncEntryPoint(pfn) reinterpret_cast<uintptr_t>(pfn)
#endif

#ifndef _ASSERTE_ALL_BUILDS
#define _ASSERTE_ALL_BUILDS(condition) \
    do \
    { \
        if (!(condition)) \
        { \
            abort(); \
        } \
    } while (false)
#endif

EXTERN_C void JIT_PatchedCodeStart();
EXTERN_C void JIT_PatchedCodeLast();
#if !defined(TARGET_X86)
EXTERN_C void JIT_WriteBarrier(Object** dst, Object* ref);
EXTERN_C void JIT_WriteBarrier_End();
#endif // !TARGET_X86

#if !defined(TARGET_X86)
EXTERN_C void JIT_CheckedWriteBarrier(Object** dst, Object* ref);
EXTERN_C void JIT_CheckedWriteBarrier_End();
EXTERN_C uint8_t RhpAssignRefAVLocation;
EXTERN_C uint8_t RhpCheckedAssignRefAVLocation;
#endif // !TARGET_X86

#if defined(TARGET_AMD64)
EXTERN_C void RhpAssignRef(Object** dst, Object* ref);
EXTERN_C void* JIT_WriteBarrier_Loc;
#ifdef _DEBUG
EXTERN_C void JIT_WriteBarrier_Debug(Object** dst, Object* ref);
EXTERN_C void JIT_WriteBarrier_Debug_End();
#endif // _DEBUG
#elif defined(TARGET_ARM64)
EXTERN_C void RhpAssignRefArm64(Object** dst, Object* ref);
EXTERN_C void RhpCheckedAssignRefArm64(Object** dst, Object* ref);
#elif defined(TARGET_ARM)
EXTERN_C void RhpAssignRef(Object** dst, Object* ref);
EXTERN_C void RhpCheckedAssignRef(Object** dst, Object* ref);
#elif defined(TARGET_LOONGARCH64)
EXTERN_C void RhpAssignRefLoongArch64(Object** dst, Object* ref);
EXTERN_C void RhpCheckedAssignRef(Object** dst, Object* ref);
#elif defined(TARGET_RISCV64)
EXTERN_C void RhpAssignRefRiscV64(Object** dst, Object* ref);
EXTERN_C void RhpCheckedAssignRef(Object** dst, Object* ref);
#elif defined(TARGET_X86)
#define X86_WRITE_BARRIER_REGISTER(reg) \
    EXTERN_C void STDCALL JIT_WriteBarrier##reg(); \
    EXTERN_C void STDCALL JIT_CheckedWriteBarrier##reg(); \
    EXTERN_C void STDCALL JIT_DebugWriteBarrier##reg(); \
    EXTERN_C void FASTCALL RhpAssignRef##reg(Object**, Object*); \
    EXTERN_C void FASTCALL RhpCheckedAssignRef##reg(Object**, Object*); \
    EXTERN_C uint8_t RhpAssignRef##reg##AVLocation; \
    EXTERN_C uint8_t RhpCheckedAssignRef##reg##AVLocation;

X86_WRITE_BARRIER_REGISTER(EAX)
X86_WRITE_BARRIER_REGISTER(ECX)
X86_WRITE_BARRIER_REGISTER(EBX)
X86_WRITE_BARRIER_REGISTER(ESI)
X86_WRITE_BARRIER_REGISTER(EDI)
X86_WRITE_BARRIER_REGISTER(EBP)
#undef X86_WRITE_BARRIER_REGISTER

EXTERN_C void FASTCALL RhpAssignRef(Object**, Object*);
EXTERN_C void FASTCALL RhpCheckedAssignRef(Object**, Object*);
EXTERN_C uint8_t RhpAssignRefAVLocation;
EXTERN_C uint8_t RhpCheckedAssignRefAVLocation;
EXTERN_C void STDCALL JIT_WriteBarrierGroup();
EXTERN_C void STDCALL JIT_WriteBarrierGroup_End();
EXTERN_C void STDCALL JIT_PatchedWriteBarrierGroup();
EXTERN_C void STDCALL JIT_PatchedWriteBarrierGroup_End();
#ifdef _DEBUG
EXTERN_C void STDCALL WriteBarrierAssert(void* destination, void* reference)
{
    GCToEEInterface::WriteBarrierAssert(destination, reference);
}
#endif // _DEBUG
#endif

WriteBarrierServices g_writeBarrierServices;

namespace
{
uint8_t* GetWriteBarrierInstructionAddress(uintptr_t address)
{
#ifdef TARGET_ARM
    address &= ~static_cast<uintptr_t>(THUMB_CODE);
#endif // TARGET_ARM
    return reinterpret_cast<uint8_t*>(address);
}

}

WriteBarrierServices::WriteBarrierServices() :
    m_codeCopy(nullptr),
    m_codeStart(GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_PatchedCodeStart)))
{
}

WriteBarrierRuntimeModeHolder::WriteBarrierRuntimeModeHolder(bool enterMode) :
    m_modeChanged(enterMode && GCToEEInterface::EnterWriteBarrierPatchMode())
{
}

WriteBarrierRuntimeModeHolder::~WriteBarrierRuntimeModeHolder()
{
    GCToEEInterface::ExitWriteBarrierPatchMode(m_modeChanged);
}

uint8_t* WriteBarrierServices::GetExecutableAddress(uint8_t* source)
{
    if (!GCToEEInterface::IsWriteBarrierCodeCopyEnabled())
    {
        return source;
    }

    if (m_codeCopy == nullptr)
    {
        m_codeCopy = GCToEEInterface::GetWriteBarrierCodeCopy();
        assert(m_codeCopy != nullptr);
    }

    return m_codeCopy + (source - m_codeStart);
}

void WriteBarrierServices::PublishWriteBarrierHelpers()
{
#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_ARM) || \
    defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    WriteBarrierHelperDescriptor helpers = {};
    helpers.code.source_start = m_codeStart;
    helpers.code.executable_start = GCToEEInterface::IsWriteBarrierCodeCopyEnabled()
        ? GCToEEInterface::GetWriteBarrierCodeCopy()
        : m_codeStart;
    helpers.code.size =
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_PatchedCodeLast)) - m_codeStart;
    _ASSERTE_ALL_BUILDS(helpers.code.size > 0);
    _ASSERTE_ALL_BUILDS(helpers.code.size < 0x1000);

    if (GCToEEInterface::IsWriteBarrierCodeCopyEnabled())
    {
        m_codeCopy = helpers.code.executable_start;
        GCToEEInterface::CopyWriteBarrierCode(m_codeCopy, m_codeStart, helpers.code.size);
        helpers.assign_ref = GetExecutableAddress(
            reinterpret_cast<uint8_t*>(GetEEFuncEntryPoint(JIT_WriteBarrier)));
#if !defined(TARGET_AMD64)
        helpers.checked_assign_ref = GetExecutableAddress(
            reinterpret_cast<uint8_t*>(GetEEFuncEntryPoint(JIT_CheckedWriteBarrier)));
#endif // !TARGET_AMD64
    }
    else
    {
#if defined(TARGET_AMD64)
        helpers.assign_ref = reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpAssignRef));
#elif defined(TARGET_ARM64)
        helpers.assign_ref = reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpAssignRefArm64));
        helpers.checked_assign_ref =
            reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpCheckedAssignRefArm64));
#elif defined(TARGET_ARM)
        helpers.assign_ref = reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpAssignRef));
        helpers.checked_assign_ref =
            reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpCheckedAssignRef));
#elif defined(TARGET_LOONGARCH64)
        helpers.assign_ref = reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpAssignRefLoongArch64));
        helpers.checked_assign_ref =
            reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpCheckedAssignRef));
#elif defined(TARGET_RISCV64)
        helpers.assign_ref = reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpAssignRefRiscV64));
        helpers.checked_assign_ref =
            reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpCheckedAssignRef));
#endif
    }

#ifdef TARGET_AMD64
    helpers.checked_assign_ref =
        reinterpret_cast<void*>(GetEEFuncEntryPoint(JIT_CheckedWriteBarrier));
    JIT_WriteBarrier_Loc = helpers.assign_ref;
#endif // TARGET_AMD64

    helpers.av_locations[helpers.av_location_count++] =
        reinterpret_cast<uintptr_t>(&RhpAssignRefAVLocation);
    helpers.av_locations[helpers.av_location_count++] =
        reinterpret_cast<uintptr_t>(&RhpCheckedAssignRefAVLocation);

    helpers.exception_ranges[helpers.exception_range_count++] = {
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_WriteBarrier)),
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_WriteBarrier_End))
    };
    helpers.exception_ranges[helpers.exception_range_count++] = {
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_CheckedWriteBarrier)),
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_CheckedWriteBarrier_End))
    };
#if defined(TARGET_AMD64) && defined(_DEBUG)
    helpers.exception_ranges[helpers.exception_range_count++] = {
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_WriteBarrier_Debug)),
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_WriteBarrier_Debug_End))
    };
#endif // TARGET_AMD64 && _DEBUG

    GCToEEInterface::SetWriteBarrierHelpers(helpers);
#elif defined(TARGET_X86)
    WriteBarrierHelperDescriptor helpers = {};
    helpers.code.source_start = m_codeStart;
    helpers.code.executable_start = GCToEEInterface::IsWriteBarrierCodeCopyEnabled()
        ? GCToEEInterface::GetWriteBarrierCodeCopy()
        : m_codeStart;
    helpers.code.size =
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_PatchedCodeLast)) - m_codeStart;

    if (GCToEEInterface::IsWriteBarrierCodeCopyEnabled())
    {
        m_codeCopy = helpers.code.executable_start;
        GCToEEInterface::CopyWriteBarrierCode(m_codeCopy, m_codeStart, helpers.code.size);

#define X86_PUBLISH_WRITE_BARRIER(index, reg) \
        helpers.assign_ref_by_register[index] = GetExecutableAddress( \
            GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_WriteBarrier##reg))); \
        helpers.checked_assign_ref_by_register[index] = \
            reinterpret_cast<void*>(GetEEFuncEntryPoint(JIT_CheckedWriteBarrier##reg));

        X86_PUBLISH_WRITE_BARRIER(0, EAX)
        X86_PUBLISH_WRITE_BARRIER(1, ECX)
        X86_PUBLISH_WRITE_BARRIER(2, EBX)
        X86_PUBLISH_WRITE_BARRIER(3, ESI)
        X86_PUBLISH_WRITE_BARRIER(4, EDI)
        X86_PUBLISH_WRITE_BARRIER(5, EBP)
#undef X86_PUBLISH_WRITE_BARRIER
    }
    else
    {
#define X86_PUBLISH_WRITE_BARRIER(index, reg) \
        helpers.assign_ref_by_register[index] = \
            reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpAssignRef##reg)); \
        helpers.checked_assign_ref_by_register[index] = \
            reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpCheckedAssignRef##reg));

        X86_PUBLISH_WRITE_BARRIER(0, EAX)
        X86_PUBLISH_WRITE_BARRIER(1, ECX)
        X86_PUBLISH_WRITE_BARRIER(2, EBX)
        X86_PUBLISH_WRITE_BARRIER(3, ESI)
        X86_PUBLISH_WRITE_BARRIER(4, EDI)
        X86_PUBLISH_WRITE_BARRIER(5, EBP)
#undef X86_PUBLISH_WRITE_BARRIER
    }

    helpers.assign_ref = reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpAssignRef));
    helpers.checked_assign_ref =
        reinterpret_cast<void*>(GetEEFuncEntryPoint(RhpCheckedAssignRef));

    helpers.av_locations[helpers.av_location_count++] =
        reinterpret_cast<uintptr_t>(&RhpAssignRefAVLocation);
#define X86_PUBLISH_AV_LOCATION(reg) \
    helpers.av_locations[helpers.av_location_count++] = \
        reinterpret_cast<uintptr_t>(&RhpAssignRef##reg##AVLocation);
    X86_PUBLISH_AV_LOCATION(EAX)
    X86_PUBLISH_AV_LOCATION(ECX)
    X86_PUBLISH_AV_LOCATION(EBX)
    X86_PUBLISH_AV_LOCATION(ESI)
    X86_PUBLISH_AV_LOCATION(EDI)
    X86_PUBLISH_AV_LOCATION(EBP)
#undef X86_PUBLISH_AV_LOCATION

    helpers.av_locations[helpers.av_location_count++] =
        reinterpret_cast<uintptr_t>(&RhpCheckedAssignRefAVLocation);
#define X86_PUBLISH_AV_LOCATION(reg) \
    helpers.av_locations[helpers.av_location_count++] = \
        reinterpret_cast<uintptr_t>(&RhpCheckedAssignRef##reg##AVLocation);
    X86_PUBLISH_AV_LOCATION(EAX)
    X86_PUBLISH_AV_LOCATION(ECX)
    X86_PUBLISH_AV_LOCATION(EBX)
    X86_PUBLISH_AV_LOCATION(ESI)
    X86_PUBLISH_AV_LOCATION(EDI)
    X86_PUBLISH_AV_LOCATION(EBP)
#undef X86_PUBLISH_AV_LOCATION

    helpers.exception_ranges[helpers.exception_range_count++] = {
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_WriteBarrierGroup)),
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_WriteBarrierGroup_End))
    };
    helpers.exception_ranges[helpers.exception_range_count++] = {
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_PatchedWriteBarrierGroup)),
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_PatchedWriteBarrierGroup_End))
    };

    GCToEEInterface::SetWriteBarrierHelpers(helpers);
#endif
}

#if defined(TARGET_AMD64) || defined(TARGET_ARM64)

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

#if !defined(WRITE_BARRIER_PATCHES_INLINE)
EXTERN_C void JIT_WriteBarrier_Table_End();
#endif // !WRITE_BARRIER_PATCHES_INLINE

#if defined(WRITE_BARRIER_PATCHES_INLINE)
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_PreGrow64_Patch_Label_CardBundleTable();
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_PostGrow64_Patch_Label_CardBundleTable();
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_SVR64_PatchLabel_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_SVR64_PatchLabel_CardBundleTable();
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#endif // FEATURE_SVR_GC

EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionShrDest();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_RegionShrSrc();
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_Byte_Region64_Patch_Label_CardBundleTable();
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionShrDest();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_RegionShrSrc();
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_Bit_Region64_Patch_Label_CardBundleTable();
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_PreGrow64_Patch_Label_CardBundleTable();
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_PostGrow64_Patch_Label_CardBundleTable();
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

#ifdef FEATURE_SVR_GC
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_SVR64_PatchLabel_CardBundleTable();
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
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
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionShrDest();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_RegionShrSrc();
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_WriteWatch_Bit_Region64_Patch_Label_CardBundleTable();
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#else // WRITE_BARRIER_PATCHES_INLINE

EXTERN_C void JIT_WriteBarrier_Patch_Label_WriteWatchTable();
EXTERN_C void JIT_WriteBarrier_Patch_Label_RegionToGeneration();
EXTERN_C void JIT_WriteBarrier_Patch_Label_RegionShr();
EXTERN_C void JIT_WriteBarrier_Patch_Label_Lower();
EXTERN_C void JIT_WriteBarrier_Patch_Label_Upper();
EXTERN_C void JIT_WriteBarrier_Patch_Label_CardTable();
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_Patch_Label_CardBundleTable();
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN_C void JIT_WriteBarrier_Patch_Label_LowestAddress();
EXTERN_C void JIT_WriteBarrier_Patch_Label_HighestAddress();
#if defined(WRITE_BARRIER_CHECK)
EXTERN_C void JIT_WriteBarrier_Patch_Label_GCShadow();
EXTERN_C void JIT_WriteBarrier_Patch_Label_GCShadowEnd();
#endif // WRITE_BARRIER_CHECK
#endif // WRITE_BARRIER_PATCHES_INLINE

namespace
{
#if defined(WRITE_BARRIER_PATCHES_INLINE)
uint8_t* CalculateWriteBarrierPatchLocation(
    uint8_t* patchBase,
    uint8_t* functionStart,
    uint8_t* patchLabel,
    size_t inlineOffset)
{
    _ASSERTE_ALL_BUILDS(patchLabel >= functionStart);
    return patchBase + (patchLabel - functionStart) + inlineOffset;
}
#endif // WRITE_BARRIER_PATCHES_INLINE

}

WriteBarrierCodeDescriptor WriteBarrierServices::GetWriteBarrierCode(WriteBarrierType writeBarrier)
{
// Marked assembly functions use LEAF_END_MARKED, which provides a public end label
// without requiring unwind information.
#define MARKED_WRITE_BARRIER(pfn) \
    { \
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(pfn)), \
        GetExecutableAddress(reinterpret_cast<uint8_t*>(GetEEFuncEntryPoint(pfn))), \
        static_cast<size_t>( \
            GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(pfn##_End)) - \
            GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(pfn))) \
    }

    switch (writeBarrier)
    {
        case WRITE_BARRIER_PREGROW64:
            return MARKED_WRITE_BARRIER(JIT_WriteBarrier_PreGrow64);
        case WRITE_BARRIER_POSTGROW64:
            return MARKED_WRITE_BARRIER(JIT_WriteBarrier_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            return MARKED_WRITE_BARRIER(JIT_WriteBarrier_SVR64);
#endif // FEATURE_SVR_GC
        case WRITE_BARRIER_BYTE_REGIONS64:
            return MARKED_WRITE_BARRIER(JIT_WriteBarrier_Byte_Region64);
        case WRITE_BARRIER_BIT_REGIONS64:
            return MARKED_WRITE_BARRIER(JIT_WriteBarrier_Bit_Region64);
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            return MARKED_WRITE_BARRIER(JIT_WriteBarrier_WriteWatch_PreGrow64);
        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            return MARKED_WRITE_BARRIER(JIT_WriteBarrier_WriteWatch_PostGrow64);
#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            return MARKED_WRITE_BARRIER(JIT_WriteBarrier_WriteWatch_SVR64);
#endif // FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
            return MARKED_WRITE_BARRIER(JIT_WriteBarrier_WriteWatch_Byte_Region64);
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            return MARKED_WRITE_BARRIER(JIT_WriteBarrier_WriteWatch_Bit_Region64);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_BUFFER:
        {
            WriteBarrierCodeDescriptor descriptor = MARKED_WRITE_BARRIER(JIT_WriteBarrier);
#if !defined(WRITE_BARRIER_PATCHES_INLINE)
            descriptor.size =
                static_cast<size_t>(GetWriteBarrierInstructionAddress(
                    GetEEFuncEntryPoint(JIT_WriteBarrier_Table_End)) -
                    descriptor.source_start);
#endif // !WRITE_BARRIER_PATCHES_INLINE
            return descriptor;
        }
        default:
            UNREACHABLE_MSG("unexpected write barrier type");
    }

#undef MARKED_WRITE_BARRIER
}

uint8_t* WriteBarrierServices::GetWriteBarrierPatchLocation(
    WriteBarrierType writeBarrier,
    WriteBarrierPatch patch)
{
    uint8_t* patchBase = GetWriteBarrierCode(WRITE_BARRIER_BUFFER).executable_start;

#if defined(WRITE_BARRIER_PATCHES_INLINE)
#define PATCH_LOCATION(func, label, offset) \
    CalculateWriteBarrierPatchLocation( \
        patchBase, \
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(func)), \
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(func##_##label)), \
        offset)

    switch (writeBarrier)
    {
        case WRITE_BARRIER_PREGROW64:
            switch (patch)
            {
                case WRITE_BARRIER_PATCH_LOWER:
                    return PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_Lower, 2);
                case WRITE_BARRIER_PATCH_CARD_TABLE:
                    return PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardTable, 2);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                case WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE:
                    return PATCH_LOCATION(JIT_WriteBarrier_PreGrow64, Patch_Label_CardBundleTable, 2);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                default:
                    UNREACHABLE_MSG("unexpected pregrow write barrier patch");
            }

        case WRITE_BARRIER_POSTGROW64:
            switch (patch)
            {
                case WRITE_BARRIER_PATCH_LOWER:
                    return PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Lower, 2);
                case WRITE_BARRIER_PATCH_UPPER:
                    return PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_Upper, 2);
                case WRITE_BARRIER_PATCH_CARD_TABLE:
                    return PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardTable, 2);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                case WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE:
                    return PATCH_LOCATION(JIT_WriteBarrier_PostGrow64, Patch_Label_CardBundleTable, 2);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                default:
                    UNREACHABLE_MSG("unexpected postgrow write barrier patch");
            }

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            switch (patch)
            {
                case WRITE_BARRIER_PATCH_CARD_TABLE:
                    return PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardTable, 2);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                case WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE:
                    return PATCH_LOCATION(JIT_WriteBarrier_SVR64, PatchLabel_CardBundleTable, 2);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                default:
                    UNREACHABLE_MSG("unexpected server write barrier patch");
            }
#endif // FEATURE_SVR_GC

#define REGION_WRITE_BARRIER_PATCHES(func) \
            switch (patch) \
            { \
                case WRITE_BARRIER_PATCH_REGION_TO_GENERATION: \
                    return PATCH_LOCATION(func, Patch_Label_RegionToGeneration, 2); \
                case WRITE_BARRIER_PATCH_REGION_SHR_DEST: \
                    return PATCH_LOCATION(func, Patch_Label_RegionShrDest, 3); \
                case WRITE_BARRIER_PATCH_REGION_SHR_SRC: \
                    return PATCH_LOCATION(func, Patch_Label_RegionShrSrc, 3); \
                case WRITE_BARRIER_PATCH_LOWER: \
                    return PATCH_LOCATION(func, Patch_Label_Lower, 2); \
                case WRITE_BARRIER_PATCH_UPPER: \
                    return PATCH_LOCATION(func, Patch_Label_Upper, 2); \
                case WRITE_BARRIER_PATCH_CARD_TABLE: \
                    return PATCH_LOCATION(func, Patch_Label_CardTable, 2);

#define REGION_WRITE_BARRIER_CARD_BUNDLE_PATCH(func) \
                case WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE: \
                    return PATCH_LOCATION(func, Patch_Label_CardBundleTable, 2);

#define END_REGION_WRITE_BARRIER_PATCHES(message) \
                default: \
                    UNREACHABLE_MSG(message); \
            }

        case WRITE_BARRIER_BYTE_REGIONS64:
            REGION_WRITE_BARRIER_PATCHES(JIT_WriteBarrier_Byte_Region64)
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            REGION_WRITE_BARRIER_CARD_BUNDLE_PATCH(JIT_WriteBarrier_Byte_Region64)
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            END_REGION_WRITE_BARRIER_PATCHES("unexpected byte region write barrier patch")

        case WRITE_BARRIER_BIT_REGIONS64:
            REGION_WRITE_BARRIER_PATCHES(JIT_WriteBarrier_Bit_Region64)
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            REGION_WRITE_BARRIER_CARD_BUNDLE_PATCH(JIT_WriteBarrier_Bit_Region64)
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            END_REGION_WRITE_BARRIER_PATCHES("unexpected bit region write barrier patch")

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
#define WRITE_WATCH_PATCH(func) \
                case WRITE_BARRIER_PATCH_WRITE_WATCH_TABLE: \
                    return PATCH_LOCATION(func, Patch_Label_WriteWatchTable, 2);

        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            switch (patch)
            {
                WRITE_WATCH_PATCH(JIT_WriteBarrier_WriteWatch_PreGrow64)
                case WRITE_BARRIER_PATCH_LOWER:
                    return PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_Lower, 2);
                case WRITE_BARRIER_PATCH_CARD_TABLE:
                    return PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PreGrow64, Patch_Label_CardTable, 2);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                case WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE:
                    return PATCH_LOCATION(
                        JIT_WriteBarrier_WriteWatch_PreGrow64,
                        Patch_Label_CardBundleTable,
                        2);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                default:
                    UNREACHABLE_MSG("unexpected write-watch pregrow write barrier patch");
            }

        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            switch (patch)
            {
                WRITE_WATCH_PATCH(JIT_WriteBarrier_WriteWatch_PostGrow64)
                case WRITE_BARRIER_PATCH_LOWER:
                    return PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_Lower, 2);
                case WRITE_BARRIER_PATCH_UPPER:
                    return PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_Upper, 2);
                case WRITE_BARRIER_PATCH_CARD_TABLE:
                    return PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_PostGrow64, Patch_Label_CardTable, 2);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                case WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE:
                    return PATCH_LOCATION(
                        JIT_WriteBarrier_WriteWatch_PostGrow64,
                        Patch_Label_CardBundleTable,
                        2);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                default:
                    UNREACHABLE_MSG("unexpected write-watch postgrow write barrier patch");
            }

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            switch (patch)
            {
                case WRITE_BARRIER_PATCH_WRITE_WATCH_TABLE:
                    return PATCH_LOCATION(
                        JIT_WriteBarrier_WriteWatch_SVR64,
                        PatchLabel_WriteWatchTable,
                        2);
                case WRITE_BARRIER_PATCH_CARD_TABLE:
                    return PATCH_LOCATION(JIT_WriteBarrier_WriteWatch_SVR64, PatchLabel_CardTable, 2);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                case WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE:
                    return PATCH_LOCATION(
                        JIT_WriteBarrier_WriteWatch_SVR64,
                        PatchLabel_CardBundleTable,
                        2);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
                default:
                    UNREACHABLE_MSG("unexpected write-watch server write barrier patch");
            }
#endif // FEATURE_SVR_GC

#define WRITE_WATCH_REGION_PATCHES(func) \
            switch (patch) \
            { \
                WRITE_WATCH_PATCH(func) \
                case WRITE_BARRIER_PATCH_REGION_TO_GENERATION: \
                    return PATCH_LOCATION(func, Patch_Label_RegionToGeneration, 2); \
                case WRITE_BARRIER_PATCH_REGION_SHR_DEST: \
                    return PATCH_LOCATION(func, Patch_Label_RegionShrDest, 3); \
                case WRITE_BARRIER_PATCH_REGION_SHR_SRC: \
                    return PATCH_LOCATION(func, Patch_Label_RegionShrSrc, 3); \
                case WRITE_BARRIER_PATCH_LOWER: \
                    return PATCH_LOCATION(func, Patch_Label_Lower, 2); \
                case WRITE_BARRIER_PATCH_UPPER: \
                    return PATCH_LOCATION(func, Patch_Label_Upper, 2); \
                case WRITE_BARRIER_PATCH_CARD_TABLE: \
                    return PATCH_LOCATION(func, Patch_Label_CardTable, 2);

        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
            WRITE_WATCH_REGION_PATCHES(JIT_WriteBarrier_WriteWatch_Byte_Region64)
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            REGION_WRITE_BARRIER_CARD_BUNDLE_PATCH(JIT_WriteBarrier_WriteWatch_Byte_Region64)
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            END_REGION_WRITE_BARRIER_PATCHES("unexpected write-watch byte region write barrier patch")

        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            WRITE_WATCH_REGION_PATCHES(JIT_WriteBarrier_WriteWatch_Bit_Region64)
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            REGION_WRITE_BARRIER_CARD_BUNDLE_PATCH(JIT_WriteBarrier_WriteWatch_Bit_Region64)
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
            END_REGION_WRITE_BARRIER_PATCHES("unexpected write-watch bit region write barrier patch")

#undef WRITE_WATCH_REGION_PATCHES
#undef WRITE_WATCH_PATCH
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        default:
            UNREACHABLE_MSG("unexpected write barrier type");
    }

#undef END_REGION_WRITE_BARRIER_PATCHES
#undef REGION_WRITE_BARRIER_CARD_BUNDLE_PATCH
#undef REGION_WRITE_BARRIER_PATCHES
#undef PATCH_LOCATION

#else // WRITE_BARRIER_PATCHES_INLINE

#define TABLE_PATCH_LOCATION(name) \
    do \
    { \
        ptrdiff_t offset = \
            reinterpret_cast<uint8_t*>(JIT_WriteBarrier_Patch_Label_##name) - \
            reinterpret_cast<uint8_t*>(JIT_WriteBarrier); \
        _ASSERTE_ALL_BUILDS(offset >= 0); \
        _ASSERTE_ALL_BUILDS( \
            static_cast<size_t>(offset) == JIT_WriteBarrier_Offset_##name); \
        return patchBase + offset; \
    } while (false)

    _ASSERTE(writeBarrier == WRITE_BARRIER_BUFFER);
    switch (patch)
    {
        case WRITE_BARRIER_PATCH_WRITE_WATCH_TABLE:
            TABLE_PATCH_LOCATION(WriteWatchTable);
        case WRITE_BARRIER_PATCH_REGION_TO_GENERATION:
            TABLE_PATCH_LOCATION(RegionToGeneration);
        case WRITE_BARRIER_PATCH_REGION_SHR_DEST:
            TABLE_PATCH_LOCATION(RegionShr);
        case WRITE_BARRIER_PATCH_LOWER:
            TABLE_PATCH_LOCATION(Lower);
        case WRITE_BARRIER_PATCH_UPPER:
            TABLE_PATCH_LOCATION(Upper);
        case WRITE_BARRIER_PATCH_CARD_TABLE:
            TABLE_PATCH_LOCATION(CardTable);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        case WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE:
            TABLE_PATCH_LOCATION(CardBundleTable);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        case WRITE_BARRIER_PATCH_LOWEST_ADDRESS:
            TABLE_PATCH_LOCATION(LowestAddress);
        case WRITE_BARRIER_PATCH_HIGHEST_ADDRESS:
            TABLE_PATCH_LOCATION(HighestAddress);
#if defined(WRITE_BARRIER_CHECK)
        case WRITE_BARRIER_PATCH_GC_SHADOW:
            TABLE_PATCH_LOCATION(GCShadow);
        case WRITE_BARRIER_PATCH_GC_SHADOW_END:
            TABLE_PATCH_LOCATION(GCShadowEnd);
#endif // WRITE_BARRIER_CHECK
        default:
            UNREACHABLE_MSG("unexpected write barrier table patch");
    }

#undef TABLE_PATCH_LOCATION
#endif // WRITE_BARRIER_PATCHES_INLINE
}

uint8_t* WriteBarrierServices::GetValidatedWriteBarrierPatchLocation(
    WriteBarrierType writeBarrier,
    WriteBarrierPatch patch)
{
    uint8_t* location = GetWriteBarrierPatchLocation(writeBarrier, patch);
#if defined(WRITE_BARRIER_PATCHES_INLINE)
    if (patch == WRITE_BARRIER_PATCH_REGION_SHR_DEST || patch == WRITE_BARRIER_PATCH_REGION_SHR_SRC)
    {
        _ASSERTE_ALL_BUILDS(*reinterpret_cast<uint8_t*>(location) == 0x16);
    }
    else
    {
        _ASSERTE_ALL_BUILDS(*reinterpret_cast<uint64_t*>(location) == 0xf0f0f0f0f0f0f0f0);
    }
#endif // WRITE_BARRIER_PATCHES_INLINE
    return location;
}

WriteBarrierPatchLocations WriteBarrierServices::GetWriteBarrierPatchLocations(WriteBarrierType writeBarrier)
{
    WriteBarrierPatchLocations locations = {};

#if defined(WRITE_BARRIER_PATCHES_INLINE)
#define SET_PATCH_LOCATION(field, patch) \
    locations.field = GetValidatedWriteBarrierPatchLocation(writeBarrier, patch)

#define SET_REGION_PATCH_LOCATIONS() \
    SET_PATCH_LOCATION(region_to_generation, WRITE_BARRIER_PATCH_REGION_TO_GENERATION); \
    SET_PATCH_LOCATION(region_shr_dest, WRITE_BARRIER_PATCH_REGION_SHR_DEST); \
    SET_PATCH_LOCATION(region_shr_src, WRITE_BARRIER_PATCH_REGION_SHR_SRC); \
    SET_PATCH_LOCATION(lower_bound, WRITE_BARRIER_PATCH_LOWER); \
    SET_PATCH_LOCATION(upper_bound, WRITE_BARRIER_PATCH_UPPER); \
    SET_PATCH_LOCATION(card_table, WRITE_BARRIER_PATCH_CARD_TABLE)

#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#define SET_CARD_BUNDLE_PATCH_LOCATION() \
    SET_PATCH_LOCATION(card_bundle_table, WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE)
#else
#define SET_CARD_BUNDLE_PATCH_LOCATION()
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

    switch (writeBarrier)
    {
        case WRITE_BARRIER_PREGROW64:
            SET_PATCH_LOCATION(lower_bound, WRITE_BARRIER_PATCH_LOWER);
            SET_PATCH_LOCATION(card_table, WRITE_BARRIER_PATCH_CARD_TABLE);
            SET_CARD_BUNDLE_PATCH_LOCATION();
            break;

        case WRITE_BARRIER_POSTGROW64:
            SET_PATCH_LOCATION(lower_bound, WRITE_BARRIER_PATCH_LOWER);
            SET_PATCH_LOCATION(upper_bound, WRITE_BARRIER_PATCH_UPPER);
            SET_PATCH_LOCATION(card_table, WRITE_BARRIER_PATCH_CARD_TABLE);
            SET_CARD_BUNDLE_PATCH_LOCATION();
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_SVR64:
            SET_PATCH_LOCATION(card_table, WRITE_BARRIER_PATCH_CARD_TABLE);
            SET_CARD_BUNDLE_PATCH_LOCATION();
            break;
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_BYTE_REGIONS64:
        case WRITE_BARRIER_BIT_REGIONS64:
            SET_REGION_PATCH_LOCATIONS();
            SET_CARD_BUNDLE_PATCH_LOCATION();
            break;

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        case WRITE_BARRIER_WRITE_WATCH_PREGROW64:
            SET_PATCH_LOCATION(write_watch_table, WRITE_BARRIER_PATCH_WRITE_WATCH_TABLE);
            SET_PATCH_LOCATION(lower_bound, WRITE_BARRIER_PATCH_LOWER);
            SET_PATCH_LOCATION(card_table, WRITE_BARRIER_PATCH_CARD_TABLE);
            SET_CARD_BUNDLE_PATCH_LOCATION();
            break;

        case WRITE_BARRIER_WRITE_WATCH_POSTGROW64:
            SET_PATCH_LOCATION(write_watch_table, WRITE_BARRIER_PATCH_WRITE_WATCH_TABLE);
            SET_PATCH_LOCATION(lower_bound, WRITE_BARRIER_PATCH_LOWER);
            SET_PATCH_LOCATION(upper_bound, WRITE_BARRIER_PATCH_UPPER);
            SET_PATCH_LOCATION(card_table, WRITE_BARRIER_PATCH_CARD_TABLE);
            SET_CARD_BUNDLE_PATCH_LOCATION();
            break;

#ifdef FEATURE_SVR_GC
        case WRITE_BARRIER_WRITE_WATCH_SVR64:
            SET_PATCH_LOCATION(write_watch_table, WRITE_BARRIER_PATCH_WRITE_WATCH_TABLE);
            SET_PATCH_LOCATION(card_table, WRITE_BARRIER_PATCH_CARD_TABLE);
            SET_CARD_BUNDLE_PATCH_LOCATION();
            break;
#endif // FEATURE_SVR_GC

        case WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64:
        case WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64:
            SET_PATCH_LOCATION(write_watch_table, WRITE_BARRIER_PATCH_WRITE_WATCH_TABLE);
            SET_REGION_PATCH_LOCATIONS();
            SET_CARD_BUNDLE_PATCH_LOCATION();
            break;
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

        default:
            UNREACHABLE_MSG("unexpected write barrier type");
    }

#undef SET_CARD_BUNDLE_PATCH_LOCATION
#undef SET_REGION_PATCH_LOCATIONS
#undef SET_PATCH_LOCATION

#else // WRITE_BARRIER_PATCHES_INLINE
    writeBarrier = WRITE_BARRIER_BUFFER;
    locations.write_watch_table =
        GetWriteBarrierPatchLocation(writeBarrier, WRITE_BARRIER_PATCH_WRITE_WATCH_TABLE);
    locations.region_to_generation =
        GetWriteBarrierPatchLocation(writeBarrier, WRITE_BARRIER_PATCH_REGION_TO_GENERATION);
    locations.region_shr_dest =
        GetWriteBarrierPatchLocation(writeBarrier, WRITE_BARRIER_PATCH_REGION_SHR_DEST);
    locations.lower_bound = GetWriteBarrierPatchLocation(writeBarrier, WRITE_BARRIER_PATCH_LOWER);
    locations.upper_bound = GetWriteBarrierPatchLocation(writeBarrier, WRITE_BARRIER_PATCH_UPPER);
    locations.card_table = GetWriteBarrierPatchLocation(writeBarrier, WRITE_BARRIER_PATCH_CARD_TABLE);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    locations.card_bundle_table =
        GetWriteBarrierPatchLocation(writeBarrier, WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#endif // WRITE_BARRIER_PATCHES_INLINE

    return locations;
}

void WriteBarrierServices::ValidateWriteBarrierLayout()
{
    // Ensure that the generic JIT_WriteBarrier function buffer is large enough to hold every implementation.
    size_t writeBarrierBufferSize = GetWriteBarrierCode(WRITE_BARRIER_BUFFER).size;

#define VALIDATE_WRITE_BARRIER_SIZE(writeBarrier) \
    _ASSERTE_ALL_BUILDS(writeBarrierBufferSize >= GetWriteBarrierCode(writeBarrier).size)

    VALIDATE_WRITE_BARRIER_SIZE(WRITE_BARRIER_PREGROW64);
    VALIDATE_WRITE_BARRIER_SIZE(WRITE_BARRIER_POSTGROW64);
#ifdef FEATURE_SVR_GC
    VALIDATE_WRITE_BARRIER_SIZE(WRITE_BARRIER_SVR64);
#endif // FEATURE_SVR_GC
    VALIDATE_WRITE_BARRIER_SIZE(WRITE_BARRIER_BYTE_REGIONS64);
    VALIDATE_WRITE_BARRIER_SIZE(WRITE_BARRIER_BIT_REGIONS64);
#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    VALIDATE_WRITE_BARRIER_SIZE(WRITE_BARRIER_WRITE_WATCH_PREGROW64);
    VALIDATE_WRITE_BARRIER_SIZE(WRITE_BARRIER_WRITE_WATCH_POSTGROW64);
#ifdef FEATURE_SVR_GC
    VALIDATE_WRITE_BARRIER_SIZE(WRITE_BARRIER_WRITE_WATCH_SVR64);
#endif // FEATURE_SVR_GC
    VALIDATE_WRITE_BARRIER_SIZE(WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64);
    VALIDATE_WRITE_BARRIER_SIZE(WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64);
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#undef VALIDATE_WRITE_BARRIER_SIZE

#if defined(WRITE_BARRIER_PATCHES_INLINE) && !defined(CODECOVERAGE)
// These values can be updated while the EE is running, so their locations must be naturally aligned.
#define VALIDATE_PATCH_ALIGNMENT(writeBarrier, patch) \
    _ASSERTE_ALL_BUILDS( \
        (reinterpret_cast<uintptr_t>(GetWriteBarrierPatchLocation(writeBarrier, patch)) & 0x7) == 0)

    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_PREGROW64, WRITE_BARRIER_PATCH_LOWER);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_PREGROW64, WRITE_BARRIER_PATCH_CARD_TABLE);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_PREGROW64, WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_POSTGROW64, WRITE_BARRIER_PATCH_LOWER);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_POSTGROW64, WRITE_BARRIER_PATCH_UPPER);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_POSTGROW64, WRITE_BARRIER_PATCH_CARD_TABLE);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_POSTGROW64, WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

#ifdef FEATURE_SVR_GC
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_SVR64, WRITE_BARRIER_PATCH_CARD_TABLE);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_SVR64, WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#endif // FEATURE_SVR_GC

    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_BYTE_REGIONS64, WRITE_BARRIER_PATCH_REGION_TO_GENERATION);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_BYTE_REGIONS64, WRITE_BARRIER_PATCH_LOWER);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_BYTE_REGIONS64, WRITE_BARRIER_PATCH_UPPER);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_BYTE_REGIONS64, WRITE_BARRIER_PATCH_CARD_TABLE);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_BYTE_REGIONS64, WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_BIT_REGIONS64, WRITE_BARRIER_PATCH_REGION_TO_GENERATION);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_BIT_REGIONS64, WRITE_BARRIER_PATCH_LOWER);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_BIT_REGIONS64, WRITE_BARRIER_PATCH_UPPER);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_BIT_REGIONS64, WRITE_BARRIER_PATCH_CARD_TABLE);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_BIT_REGIONS64, WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

#ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
    VALIDATE_PATCH_ALIGNMENT(
        WRITE_BARRIER_WRITE_WATCH_PREGROW64,
        WRITE_BARRIER_PATCH_WRITE_WATCH_TABLE);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_WRITE_WATCH_PREGROW64, WRITE_BARRIER_PATCH_LOWER);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_WRITE_WATCH_PREGROW64, WRITE_BARRIER_PATCH_CARD_TABLE);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    VALIDATE_PATCH_ALIGNMENT(
        WRITE_BARRIER_WRITE_WATCH_PREGROW64,
        WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

    VALIDATE_PATCH_ALIGNMENT(
        WRITE_BARRIER_WRITE_WATCH_POSTGROW64,
        WRITE_BARRIER_PATCH_WRITE_WATCH_TABLE);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_WRITE_WATCH_POSTGROW64, WRITE_BARRIER_PATCH_LOWER);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_WRITE_WATCH_POSTGROW64, WRITE_BARRIER_PATCH_UPPER);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_WRITE_WATCH_POSTGROW64, WRITE_BARRIER_PATCH_CARD_TABLE);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    VALIDATE_PATCH_ALIGNMENT(
        WRITE_BARRIER_WRITE_WATCH_POSTGROW64,
        WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

#ifdef FEATURE_SVR_GC
    VALIDATE_PATCH_ALIGNMENT(
        WRITE_BARRIER_WRITE_WATCH_SVR64,
        WRITE_BARRIER_PATCH_WRITE_WATCH_TABLE);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_WRITE_WATCH_SVR64, WRITE_BARRIER_PATCH_CARD_TABLE);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    VALIDATE_PATCH_ALIGNMENT(
        WRITE_BARRIER_WRITE_WATCH_SVR64,
        WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#endif // FEATURE_SVR_GC

    VALIDATE_PATCH_ALIGNMENT(
        WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64,
        WRITE_BARRIER_PATCH_REGION_TO_GENERATION);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64, WRITE_BARRIER_PATCH_LOWER);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64, WRITE_BARRIER_PATCH_UPPER);
    VALIDATE_PATCH_ALIGNMENT(
        WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64,
        WRITE_BARRIER_PATCH_CARD_TABLE);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    VALIDATE_PATCH_ALIGNMENT(
        WRITE_BARRIER_WRITE_WATCH_BYTE_REGIONS64,
        WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES

    VALIDATE_PATCH_ALIGNMENT(
        WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64,
        WRITE_BARRIER_PATCH_REGION_TO_GENERATION);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64, WRITE_BARRIER_PATCH_LOWER);
    VALIDATE_PATCH_ALIGNMENT(WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64, WRITE_BARRIER_PATCH_UPPER);
    VALIDATE_PATCH_ALIGNMENT(
        WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64,
        WRITE_BARRIER_PATCH_CARD_TABLE);
#ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
    VALIDATE_PATCH_ALIGNMENT(
        WRITE_BARRIER_WRITE_WATCH_BIT_REGIONS64,
        WRITE_BARRIER_PATCH_CARD_BUNDLE_TABLE);
#endif // FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
#endif // FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP

#undef VALIDATE_PATCH_ALIGNMENT
#endif // WRITE_BARRIER_PATCHES_INLINE && !CODECOVERAGE
}

bool WriteBarrierServices::UpdateWriteBarrierPointer(WriteBarrierPatch patch, uint8_t* value)
{
    uint8_t* location = GetWriteBarrierPatchLocation(WRITE_BARRIER_BUFFER, patch);
    uint64_t newValue = reinterpret_cast<uintptr_t>(value);
    if (*reinterpret_cast<uint64_t*>(location) == newValue)
    {
        return false;
    }

    GCToEEInterface::UpdateWriteBarrierValue(location, newValue, sizeof(newValue));
    return true;
}

bool WriteBarrierServices::UpdateWriteBarrierImplementationState(const WriteBarrierParameters& state)
{
#if defined(TARGET_ARM64)
    bool valueChanged = UpdateWriteBarrierPointer(WRITE_BARRIER_PATCH_LOWEST_ADDRESS, state.lowest_address);
    valueChanged |= UpdateWriteBarrierPointer(WRITE_BARRIER_PATCH_HIGHEST_ADDRESS, state.highest_address);
#if defined(WRITE_BARRIER_CHECK)
    valueChanged |= UpdateWriteBarrierPointer(WRITE_BARRIER_PATCH_GC_SHADOW, g_GCShadow);
    valueChanged |= UpdateWriteBarrierPointer(WRITE_BARRIER_PATCH_GC_SHADOW_END, g_GCShadowEnd);
#endif // WRITE_BARRIER_CHECK
    return valueChanged;
#else
    UNREFERENCED_PARAMETER(state);
    return false;
#endif // TARGET_ARM64
}

#elif defined(TARGET_ARM)

EXTERN_C void JIT_WriteBarrier(Object** dst, Object* ref);
EXTERN_C void JIT_WriteBarrier_End();
EXTERN_C void JIT_CheckedWriteBarrier(Object** dst, Object* ref);
EXTERN_C void JIT_CheckedWriteBarrier_End();

WriteBarrierCodeDescriptor WriteBarrierServices::GetWriteBarrierCode(WriteBarrierType writeBarrier)
{
#define MARKED_WRITE_BARRIER(pfn) \
    { \
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(pfn)), \
        GetExecutableAddress(GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(pfn))), \
        static_cast<size_t>( \
            GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(pfn##_End)) - \
            GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(pfn))) \
    }

    switch (writeBarrier)
    {
        case WRITE_BARRIER_BUFFER:
            return MARKED_WRITE_BARRIER(JIT_WriteBarrier);
        case WRITE_BARRIER_CHECKED_BUFFER:
            return MARKED_WRITE_BARRIER(JIT_CheckedWriteBarrier);
        default:
            UNREACHABLE_MSG("unexpected ARM write barrier type");
    }

#undef MARKED_WRITE_BARRIER
}

WriteBarrierPatchLocations WriteBarrierServices::GetWriteBarrierPatchLocations(WriteBarrierType)
{
    UNREACHABLE_MSG("ARM write barriers use assembly-generated patch descriptors");
}

void WriteBarrierServices::ValidateWriteBarrierLayout()
{
}

bool WriteBarrierServices::UpdateWriteBarrierImplementationState(const WriteBarrierParameters&)
{
    return false;
}

#elif defined(TARGET_X86)

extern "C" void STDCALL JIT_WriteBarrierReg_PreGrow();
extern "C" void STDCALL JIT_WriteBarrierReg_PostGrow();

namespace
{
constexpr size_t PreGrowSize = 34;
constexpr size_t PostGrowSize = 42;
constexpr size_t DestinationSize = 48;
}

WriteBarrierCodeDescriptor WriteBarrierServices::GetWriteBarrierCode(WriteBarrierType writeBarrier)
{
#define WRITE_BARRIER_TEMPLATE(pfn, codeSize) \
    { \
        reinterpret_cast<uint8_t*>(pfn), \
        reinterpret_cast<uint8_t*>(pfn), \
        codeSize \
    }

#define WRITE_BARRIER_DESTINATION(pfn) \
    { \
        reinterpret_cast<uint8_t*>(pfn), \
        GetExecutableAddress(reinterpret_cast<uint8_t*>(pfn)), \
        DestinationSize \
    }

#define DEBUG_WRITE_BARRIER(pfn) \
    { \
        reinterpret_cast<uint8_t*>(pfn), \
        reinterpret_cast<uint8_t*>(pfn), \
        0 \
    }

    switch (writeBarrier)
    {
        case WRITE_BARRIER_X86_PREGROW:
            return WRITE_BARRIER_TEMPLATE(JIT_WriteBarrierReg_PreGrow, PreGrowSize);
        case WRITE_BARRIER_X86_POSTGROW:
            return WRITE_BARRIER_TEMPLATE(JIT_WriteBarrierReg_PostGrow, PostGrowSize);
        case WRITE_BARRIER_X86_EAX:
            return WRITE_BARRIER_DESTINATION(JIT_WriteBarrierEAX);
        case WRITE_BARRIER_X86_ECX:
            return WRITE_BARRIER_DESTINATION(JIT_WriteBarrierECX);
        case WRITE_BARRIER_X86_EBX:
            return WRITE_BARRIER_DESTINATION(JIT_WriteBarrierEBX);
        case WRITE_BARRIER_X86_ESI:
            return WRITE_BARRIER_DESTINATION(JIT_WriteBarrierESI);
        case WRITE_BARRIER_X86_EDI:
            return WRITE_BARRIER_DESTINATION(JIT_WriteBarrierEDI);
        case WRITE_BARRIER_X86_EBP:
            return WRITE_BARRIER_DESTINATION(JIT_WriteBarrierEBP);
#ifdef WRITE_BARRIER_CHECK
        case WRITE_BARRIER_X86_DEBUG_EAX:
            return DEBUG_WRITE_BARRIER(JIT_DebugWriteBarrierEAX);
        case WRITE_BARRIER_X86_DEBUG_ECX:
            return DEBUG_WRITE_BARRIER(JIT_DebugWriteBarrierECX);
        case WRITE_BARRIER_X86_DEBUG_EBX:
            return DEBUG_WRITE_BARRIER(JIT_DebugWriteBarrierEBX);
        case WRITE_BARRIER_X86_DEBUG_ESI:
            return DEBUG_WRITE_BARRIER(JIT_DebugWriteBarrierESI);
        case WRITE_BARRIER_X86_DEBUG_EDI:
            return DEBUG_WRITE_BARRIER(JIT_DebugWriteBarrierEDI);
        case WRITE_BARRIER_X86_DEBUG_EBP:
            return DEBUG_WRITE_BARRIER(JIT_DebugWriteBarrierEBP);
#endif // WRITE_BARRIER_CHECK
        default:
            UNREACHABLE_MSG("unexpected x86 write barrier type");
    }

#undef DEBUG_WRITE_BARRIER
#undef WRITE_BARRIER_DESTINATION
#undef WRITE_BARRIER_TEMPLATE
}

WriteBarrierPatchLocations WriteBarrierServices::GetWriteBarrierPatchLocations(WriteBarrierType)
{
    UNREACHABLE_MSG("x86 write barriers use fixed patch offsets");
}

void WriteBarrierServices::ValidateWriteBarrierLayout()
{
    _ASSERTE_ALL_BUILDS(
        reinterpret_cast<uint8_t*>(JIT_WriteBarrierGroup_End) -
        reinterpret_cast<uint8_t*>(JIT_WriteBarrierGroup) <
        static_cast<ptrdiff_t>(0x1000));
    _ASSERTE_ALL_BUILDS(
        reinterpret_cast<uint8_t*>(JIT_PatchedWriteBarrierGroup_End) -
        reinterpret_cast<uint8_t*>(JIT_PatchedWriteBarrierGroup) <
        static_cast<ptrdiff_t>(0x1000));
}

bool WriteBarrierServices::UpdateWriteBarrierImplementationState(const WriteBarrierParameters&)
{
    return false;
}

#elif defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

EXTERN_C void JIT_WriteBarrier_Table();
EXTERN_C void JIT_WriteBarrier_Table_End();

namespace
{
enum WriteBarrierTableEntry : size_t
{
    WRITE_BARRIER_TABLE_CARD,
    WRITE_BARRIER_TABLE_CARD_BUNDLE,
#ifdef TARGET_RISCV64
    WRITE_BARRIER_TABLE_GC_SHADOW,
    WRITE_BARRIER_TABLE_GC_SHADOW_END,
#endif // TARGET_RISCV64
    WRITE_BARRIER_TABLE_WRITE_WATCH,
    WRITE_BARRIER_TABLE_EPHEMERAL_LOW,
    WRITE_BARRIER_TABLE_EPHEMERAL_HIGH,
    WRITE_BARRIER_TABLE_LOWEST_ADDRESS,
    WRITE_BARRIER_TABLE_HIGHEST_ADDRESS,
#ifdef TARGET_LOONGARCH64
    WRITE_BARRIER_TABLE_GC_SHADOW,
    WRITE_BARRIER_TABLE_GC_SHADOW_END,
#endif // TARGET_LOONGARCH64
    WRITE_BARRIER_TABLE_COUNT
};

void UpdateWriteBarrierTable(
    uint64_t* table,
    const WriteBarrierParameters& state,
    bool isServerGC)
{
    uint8_t* ephemeralLow = state.ephemeral_low;
    uint8_t* ephemeralHigh = state.ephemeral_high;
    if (isServerGC)
    {
        ephemeralLow = nullptr;
        ephemeralHigh = reinterpret_cast<uint8_t*>(UINTPTR_MAX);
    }

    VolatileStoreWithoutBarrier(
        &table[WRITE_BARRIER_TABLE_CARD],
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(state.card_table)));
    VolatileStoreWithoutBarrier(
        &table[WRITE_BARRIER_TABLE_CARD_BUNDLE],
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(state.card_bundle_table)));
#ifdef TARGET_RISCV64
#ifdef WRITE_BARRIER_CHECK
    VolatileStoreWithoutBarrier(
        &table[WRITE_BARRIER_TABLE_GC_SHADOW],
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(g_GCShadow)));
    VolatileStoreWithoutBarrier(
        &table[WRITE_BARRIER_TABLE_GC_SHADOW_END],
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(g_GCShadowEnd)));
#else
    VolatileStoreWithoutBarrier(&table[WRITE_BARRIER_TABLE_GC_SHADOW], uint64_t{});
    VolatileStoreWithoutBarrier(&table[WRITE_BARRIER_TABLE_GC_SHADOW_END], uint64_t{});
#endif // WRITE_BARRIER_CHECK
#endif // TARGET_RISCV64
    VolatileStoreWithoutBarrier(
        &table[WRITE_BARRIER_TABLE_WRITE_WATCH],
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(state.write_watch_table)));
    VolatileStoreWithoutBarrier(
        &table[WRITE_BARRIER_TABLE_EPHEMERAL_LOW],
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(ephemeralLow)));
    VolatileStoreWithoutBarrier(
        &table[WRITE_BARRIER_TABLE_EPHEMERAL_HIGH],
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(ephemeralHigh)));
    VolatileStoreWithoutBarrier(
        &table[WRITE_BARRIER_TABLE_LOWEST_ADDRESS],
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(state.lowest_address)));
    VolatileStoreWithoutBarrier(
        &table[WRITE_BARRIER_TABLE_HIGHEST_ADDRESS],
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(state.highest_address)));
#ifdef TARGET_LOONGARCH64
#ifdef WRITE_BARRIER_CHECK
    VolatileStoreWithoutBarrier(
        &table[WRITE_BARRIER_TABLE_GC_SHADOW],
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(g_GCShadow)));
    VolatileStoreWithoutBarrier(
        &table[WRITE_BARRIER_TABLE_GC_SHADOW_END],
        static_cast<uint64_t>(reinterpret_cast<uintptr_t>(g_GCShadowEnd)));
#else
    VolatileStoreWithoutBarrier(&table[WRITE_BARRIER_TABLE_GC_SHADOW], uint64_t{});
    VolatileStoreWithoutBarrier(&table[WRITE_BARRIER_TABLE_GC_SHADOW_END], uint64_t{});
#endif // WRITE_BARRIER_CHECK
#endif // TARGET_LOONGARCH64
}
}

WriteBarrierCodeDescriptor WriteBarrierServices::GetWriteBarrierCode(WriteBarrierType writeBarrier)
{
    _ASSERTE(writeBarrier == WRITE_BARRIER_BUFFER);

    uint8_t* table =
        GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_WriteBarrier_Table));
    return {
        table,
        GetExecutableAddress(table),
        static_cast<size_t>(
            GetWriteBarrierInstructionAddress(GetEEFuncEntryPoint(JIT_WriteBarrier_Table_End)) -
            table)
    };
}

WriteBarrierPatchLocations WriteBarrierServices::GetWriteBarrierPatchLocations(WriteBarrierType)
{
#ifdef TARGET_LOONGARCH64
    UNREACHABLE_MSG("LoongArch64 write barriers use a literal table");
#else
    UNREACHABLE_MSG("RISC-V64 write barriers use a literal table");
#endif
}

void WriteBarrierServices::ValidateWriteBarrierLayout()
{
    _ASSERTE_ALL_BUILDS(
        GetWriteBarrierCode(WRITE_BARRIER_BUFFER).size ==
        WRITE_BARRIER_TABLE_COUNT * sizeof(uint64_t));
}

bool WriteBarrierServices::UpdateWriteBarrierImplementationState(const WriteBarrierParameters& state)
{
    if (!GCToEEInterface::IsWriteBarrierCodeCopyEnabled())
    {
        return false;
    }

    WriteBarrierCodeDescriptor tableCode = GetWriteBarrierCode(WRITE_BARRIER_BUFFER);
    uint64_t table[WRITE_BARRIER_TABLE_COUNT] = {};
    UpdateWriteBarrierTable(table, state, GCToEEInterface::IsServerGC());
    for (size_t index = 0; index < WRITE_BARRIER_TABLE_COUNT; index++)
    {
        GCToEEInterface::UpdateWriteBarrierValue(
            tableCode.executable_start + index * sizeof(uint64_t),
            table[index],
            sizeof(uint64_t));
    }

    return false;
}

#endif

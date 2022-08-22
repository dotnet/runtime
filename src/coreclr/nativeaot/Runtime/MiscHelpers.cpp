// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Miscellaneous unmanaged helpers called by managed code.
//

#include "common.h"
#include "CommonTypes.h"
#include "CommonMacros.h"
#include "daccess.h"
#include "PalRedhawkCommon.h"
#include "PalRedhawk.h"
#include "rhassert.h"
#include "slist.h"
#include "holder.h"
#include "Crst.h"
#include "rhbinder.h"
#include "RWLock.h"
#include "RuntimeInstance.h"
#include "regdisplay.h"
#include "gcrhinterface.h"
#include "varint.h"
#include "StackFrameIterator.h"
#include "thread.h"
#include "event.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"
#include "gcrhinterface.h"
#include "shash.h"
#include "TypeManager.h"
#include "MethodTable.h"
#include "ObjectLayout.h"
#include "slist.inl"
#include "MethodTable.inl"
#include "CommonMacros.inl"
#include "volatile.h"
#include "GCMemoryHelpers.h"
#include "GCMemoryHelpers.inl"
#include "yieldprocessornormalized.h"

COOP_PINVOKE_HELPER(void, RhDebugBreak, ())
{
    PalDebugBreak();
}

// Busy spin for the given number of iterations.
EXTERN_C NATIVEAOT_API void __cdecl RhSpinWait(int32_t iterations)
{
    ASSERT(iterations > 0);

    // limit the spin count in coop mode.
    ASSERT_MSG(iterations <= 10000 || !ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode(),
        "This is too long wait for coop mode. You must p/invoke with GC transition.");

    YieldProcessorNormalizationInfo normalizationInfo;
    YieldProcessorNormalizedForPreSkylakeCount(normalizationInfo, iterations);
}

// Yield the cpu to another thread ready to process, if one is available.
EXTERN_C NATIVEAOT_API UInt32_BOOL __cdecl RhYield()
{
    // This must be called via p/invoke -- it's a wait operation and we don't want to block thread suspension on this.
    ASSERT_MSG(!ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode(),
        "You must p/invoke to RhYield");

    return PalSwitchToThread();
}

EXTERN_C NATIVEAOT_API void __cdecl RhFlushProcessWriteBuffers()
{
    // This must be called via p/invoke -- it's a wait operation and we don't want to block thread suspension on this.
    ASSERT_MSG(!ThreadStore::GetCurrentThread()->IsCurrentThreadInCooperativeMode(),
        "You must p/invoke to RhFlushProcessWriteBuffers");

    PalFlushProcessWriteBuffers();
}

// Get the list of currently loaded Redhawk modules (as OS HMODULE handles). The caller provides a reference
// to an array of pointer-sized elements and we return the total number of modules currently loaded (whether
// that is less than, equal to or greater than the number of elements in the array). If there are more modules
// loaded than the array will hold then the array is filled to capacity and the caller can tell further
// modules are available based on the return count. It is also possible to call this method without an array,
// in which case just the module count is returned (note that it's still possible for the module count to
// increase between calls to this method).
COOP_PINVOKE_HELPER(uint32_t, RhGetLoadedOSModules, (Array * pResultArray))
{
    // Note that we depend on the fact that this is a COOP helper to make writing into an unpinned array safe.

    // If a result array is passed then it should be an array type with pointer-sized components that are not
    // GC-references.
    ASSERT(!pResultArray || pResultArray->get_EEType()->IsArray());
    ASSERT(!pResultArray || !pResultArray->get_EEType()->HasReferenceFields());
    ASSERT(!pResultArray || pResultArray->get_EEType()->get_ComponentSize() == sizeof(void*));

    uint32_t cResultArrayElements = pResultArray ? pResultArray->GetArrayLength() : 0;
    HANDLE * pResultElements = pResultArray ? (HANDLE*)(pResultArray + 1) : NULL;

    uint32_t cModules = 0;

    ReaderWriterLock::ReadHolder read(&GetRuntimeInstance()->GetTypeManagerLock());

    RuntimeInstance::OsModuleList *osModules = GetRuntimeInstance()->GetOsModuleList();

    for (RuntimeInstance::OsModuleList::Iterator iter = osModules->Begin(); iter != osModules->End(); iter++)
    {
        if (pResultArray && (cModules < cResultArrayElements))
            pResultElements[cModules] = iter->m_osModule;
        cModules++;
    }

    return cModules;
}

COOP_PINVOKE_HELPER(HANDLE, RhGetOSModuleFromPointer, (PTR_VOID pPointerVal))
{
    ICodeManager * pCodeManager = GetRuntimeInstance()->GetCodeManagerForAddress(pPointerVal);

    if (pCodeManager != NULL)
        return (HANDLE)pCodeManager->GetOsModuleHandle();

    return NULL;
}

COOP_PINVOKE_HELPER(HANDLE, RhGetOSModuleFromEEType, (MethodTable * pEEType))
{
    return pEEType->GetTypeManagerPtr()->AsTypeManager()->GetOsModuleHandle();
}

COOP_PINVOKE_HELPER(TypeManagerHandle, RhGetModuleFromEEType, (MethodTable * pEEType))
{
    return *pEEType->GetTypeManagerPtr();
}

COOP_PINVOKE_HELPER(FC_BOOL_RET, RhFindBlob, (TypeManagerHandle *pTypeManagerHandle, uint32_t blobId, uint8_t ** ppbBlob, uint32_t * pcbBlob))
{
    TypeManagerHandle typeManagerHandle = *pTypeManagerHandle;

    ReadyToRunSectionType section =
        (ReadyToRunSectionType)((uint32_t)ReadyToRunSectionType::ReadonlyBlobRegionStart + blobId);
    ASSERT(section <= ReadyToRunSectionType::ReadonlyBlobRegionEnd);

    TypeManager* pModule = typeManagerHandle.AsTypeManager();

    int length;
    void* pBlob;
    pBlob = pModule->GetModuleSection(section, &length);

    *ppbBlob = (uint8_t*)pBlob;
    *pcbBlob = (uint32_t)length;

    FC_RETURN_BOOL(pBlob != NULL);
}

COOP_PINVOKE_HELPER(void *, RhGetTargetOfUnboxingAndInstantiatingStub, (void * pUnboxStub))
{
    return GetRuntimeInstance()->GetTargetOfUnboxingAndInstantiatingStub(pUnboxStub);
}

#if TARGET_ARM
//*****************************************************************************
//  Extract the 16-bit immediate from ARM Thumb2 Instruction (format T2_N)
//*****************************************************************************
static FORCEINLINE uint16_t GetThumb2Imm16(uint16_t * p)
{
    return ((p[0] << 12) & 0xf000) |
        ((p[0] << 1) & 0x0800) |
        ((p[1] >> 4) & 0x0700) |
        ((p[1] >> 0) & 0x00ff);
}

//*****************************************************************************
//  Extract the 32-bit immediate from movw/movt sequence
//*****************************************************************************
inline uint32_t GetThumb2Mov32(uint16_t * p)
{
    // Make sure we are decoding movw/movt sequence
    ASSERT((*(p + 0) & 0xFBF0) == 0xF240);
    ASSERT((*(p + 2) & 0xFBF0) == 0xF2C0);

    return (uint32_t)GetThumb2Imm16(p) + ((uint32_t)GetThumb2Imm16(p + 2) << 16);
}

//*****************************************************************************
//  Extract the 24-bit distance from a B/BL instruction
//*****************************************************************************
inline int32_t GetThumb2BlRel24(uint16_t * p)
{
    uint16_t Opcode0 = p[0];
    uint16_t Opcode1 = p[1];

    uint32_t S = Opcode0 >> 10;
    uint32_t J2 = Opcode1 >> 11;
    uint32_t J1 = Opcode1 >> 13;

    int32_t ret =
        ((S << 24) & 0x1000000) |
        (((J1 ^ S ^ 1) << 23) & 0x0800000) |
        (((J2 ^ S ^ 1) << 22) & 0x0400000) |
        ((Opcode0 << 12) & 0x03FF000) |
        ((Opcode1 << 1) & 0x0000FFE);

    // Sign-extend and return
    return (ret << 7) >> 7;
}
#endif // TARGET_ARM

// Given a pointer to code, find out if this points to an import stub
// or unboxing stub, and if so, return the address that stub jumps to
COOP_PINVOKE_HELPER(uint8_t *, RhGetCodeTarget, (uint8_t * pCodeOrg))
{
    bool unboxingStub = false;

    // First, check the unboxing stubs regions known by the runtime (if any exist)
    if (!GetRuntimeInstance()->IsUnboxingStub(pCodeOrg))
    {
        return pCodeOrg;
    }

#ifdef TARGET_AMD64
    uint8_t * pCode = pCodeOrg;

    // is this "add rcx/rdi,8"?
    if (pCode[0] == 0x48 &&
        pCode[1] == 0x83 &&
#ifdef UNIX_AMD64_ABI
        pCode[2] == 0xc7 &&
#else
        pCode[2] == 0xc1 &&
#endif
        pCode[3] == 0x08)
    {
        // unboxing sequence
        unboxingStub = true;
        pCode += 4;
    }
    // is this an indirect jump?
    if (pCode[0] == 0xff && pCode[1] == 0x25)
    {
        // normal import stub - dist to IAT cell is relative to the point *after* the instruction
        int32_t distToIatCell = *(int32_t *)&pCode[2];
        uint8_t ** pIatCell = (uint8_t **)(pCode + 6 + distToIatCell);
        return *pIatCell;
    }
    // is this an unboxing stub followed by a relative jump?
    else if (unboxingStub && pCode[0] == 0xe9)
    {
        // relative jump - dist is relative to the point *after* the instruction
        int32_t distToTarget = *(int32_t *)&pCode[1];
        uint8_t * target = pCode + 5 + distToTarget;
        return target;
    }

#elif TARGET_X86
    uint8_t * pCode = pCodeOrg;

    // is this "add ecx,4"?
    if (pCode[0] == 0x83 && pCode[1] == 0xc1 && pCode[2] == 0x04)
    {
        // unboxing sequence
        unboxingStub = true;
        pCode += 3;
    }
    // is this an indirect jump?
    if (pCode[0] == 0xff && pCode[1] == 0x25)
    {
        // normal import stub - address of IAT follows
        uint8_t **pIatCell = *(uint8_t ***)&pCode[2];
        return *pIatCell;
    }
    // is this an unboxing stub followed by a relative jump?
    else if (unboxingStub && pCode[0] == 0xe9)
    {
        // relative jump - dist is relative to the point *after* the instruction
        int32_t distToTarget = *(int32_t *)&pCode[1];
        uint8_t * pTarget = pCode + 5 + distToTarget;
        return pTarget;
    }

#elif TARGET_ARM
    uint16_t * pCode = (uint16_t *)((size_t)pCodeOrg & ~THUMB_CODE);
    // is this "adds r0,4"?
    if (pCode[0] == 0x3004)
    {
        // unboxing sequence
        unboxingStub = true;
        pCode += 1;
    }
    // is this movw r12,#imm16; movt r12,#imm16; ldr pc,[r12]
    // or movw r12,#imm16; movt r12,#imm16; bx r12
    if  ((pCode[0] & 0xfbf0) == 0xf240 && (pCode[1] & 0x0f00) == 0x0c00
        && (pCode[2] & 0xfbf0) == 0xf2c0 && (pCode[3] & 0x0f00) == 0x0c00
        && ((pCode[4] == 0xf8dc && pCode[5] == 0xf000) || pCode[4] == 0x4760))
    {
        if (pCode[4] == 0xf8dc && pCode[5] == 0xf000)
        {
            // ldr pc,[r12]
            uint8_t **pIatCell = (uint8_t **)GetThumb2Mov32(pCode);
            return *pIatCell;
        }
        else if (pCode[4] == 0x4760)
        {
            // bx r12
            return (uint8_t *)GetThumb2Mov32(pCode);
        }
    }
    // is this an unboxing stub followed by a relative jump?
    else if (unboxingStub && (pCode[0] & 0xf800) == 0xf000 && (pCode[1] & 0xd000) == 0x9000)
    {
        int32_t distToTarget = GetThumb2BlRel24(pCode);
        uint8_t * pTarget = (uint8_t *)(pCode + 2) + distToTarget + THUMB_CODE;
        return (uint8_t *)pTarget;
    }

#elif TARGET_ARM64
    uint32_t * pCode = (uint32_t *)pCodeOrg;
    // is this "add x0,x0,#8"?
    if (pCode[0] == 0x91002000)
    {
        // unboxing sequence
        unboxingStub = true;
        pCode++;
    }
    // is this an indirect jump?
    // adrp xip0,#imm21; ldr xip0,[xip0,#imm12]; br xip0
    if ((pCode[0] & 0x9f00001f) == 0x90000010 &&
        (pCode[1] & 0xffc003ff) == 0xf9400210 &&
        pCode[2] == 0xd61f0200)
    {
        // normal import stub - dist to IAT cell is relative to (PC & ~0xfff)
        // adrp: imm = SignExtend(immhi:immlo:Zeros(12), 64);
        int64_t distToIatCell = (((((int64_t)pCode[0] & ~0x1f) << 40) >> 31) | ((pCode[0] >> 17) & 0x3000));
        // ldr: offset = LSL(ZeroExtend(imm12, 64), 3);
        distToIatCell += (pCode[1] >> 7) & 0x7ff8;
        uint8_t ** pIatCell = (uint8_t **)(((int64_t)pCode & ~0xfff) + distToIatCell);
        return *pIatCell;
    }
    // is this an unboxing stub followed by a relative jump?
    else if (unboxingStub && (pCode[0] >> 26) == 0x5)
    {
        // relative jump - dist is relative to the instruction
        // offset = SignExtend(imm26:'00', 64);
        int64_t distToTarget = ((int64_t)pCode[0] << 38) >> 36;
        return (uint8_t *)pCode + distToTarget;
    }
#else
    UNREFERENCED_PARAMETER(unboxingStub);
    PORTABILITY_ASSERT("RhGetCodeTarget");
#endif

    return pCodeOrg;
}

// Get the universal transition thunk. If the universal transition stub is called through
// the normal PE static linkage model, a jump stub would be used which may interfere with
// the custom calling convention of the universal transition thunk. So instead, a special
// api just for getting the thunk address is needed.
// TODO: On ARM this may still result in a jump stub that trashes R12. Determine if anything
//       needs to be done about that when we implement the stub for ARM.
extern "C" void RhpUniversalTransition();
COOP_PINVOKE_HELPER(void*, RhGetUniversalTransitionThunk, ())
{
    return (void*)RhpUniversalTransition;
}

extern CrstStatic g_CastCacheLock;

EXTERN_C NATIVEAOT_API void __cdecl RhpAcquireCastCacheLock()
{
    g_CastCacheLock.Enter();
}

EXTERN_C NATIVEAOT_API void __cdecl RhpReleaseCastCacheLock()
{
    g_CastCacheLock.Leave();
}

extern CrstStatic g_ThunkPoolLock;

EXTERN_C NATIVEAOT_API void __cdecl RhpAcquireThunkPoolLock()
{
    g_ThunkPoolLock.Enter();
}

EXTERN_C NATIVEAOT_API void __cdecl RhpReleaseThunkPoolLock()
{
    g_ThunkPoolLock.Leave();
}

EXTERN_C NATIVEAOT_API void __cdecl RhpGetTickCount64()
{
    PalGetTickCount64();
}

EXTERN_C int32_t __cdecl RhpCalculateStackTraceWorker(void* pOutputBuffer, uint32_t outputBufferLength, void* pAddressInCurrentFrame);

EXTERN_C NATIVEAOT_API int32_t __cdecl RhpGetCurrentThreadStackTrace(void* pOutputBuffer, uint32_t outputBufferLength, void* pAddressInCurrentFrame)
{
    // This must be called via p/invoke rather than RuntimeImport to make the stack crawlable.

    ThreadStore::GetCurrentThread()->DeferTransitionFrame();

    return RhpCalculateStackTraceWorker(pOutputBuffer, outputBufferLength, pAddressInCurrentFrame);
}

COOP_PINVOKE_HELPER(void*, RhpRegisterFrozenSegment, (void* pSegmentStart, size_t length))
{
    return RedhawkGCInterface::RegisterFrozenSegment(pSegmentStart, length);
}

COOP_PINVOKE_HELPER(void, RhpUnregisterFrozenSegment, (void* pSegmentHandle))
{
    RedhawkGCInterface::UnregisterFrozenSegment((GcSegmentHandle)pSegmentHandle);
}

COOP_PINVOKE_HELPER(void*, RhpGetModuleSection, (TypeManagerHandle *pModule, int32_t headerId, int32_t* length))
{
    return pModule->AsTypeManager()->GetModuleSection((ReadyToRunSectionType)headerId, length);
}

COOP_PINVOKE_HELPER(void, RhGetCurrentThreadStackBounds, (PTR_VOID * ppStackLow, PTR_VOID * ppStackHigh))
{
    ThreadStore::GetCurrentThread()->GetStackBounds(ppStackLow, ppStackHigh);
}

// Function to call when a thread is detached from the runtime
ThreadExitCallback g_threadExitCallback;

COOP_PINVOKE_HELPER(void, RhSetThreadExitCallback, (void * pCallback))
{
    g_threadExitCallback = (ThreadExitCallback)pCallback;
}

COOP_PINVOKE_HELPER(int32_t, RhGetProcessCpuCount, ())
{
    return PalGetProcessCpuCount();
}

#if defined(TARGET_X86) || defined(TARGET_AMD64)
EXTERN_C NATIVEAOT_API void __cdecl RhCpuIdEx(int* cpuInfo, int functionId, int subFunctionId)
{
    __cpuidex(cpuInfo, functionId, subFunctionId);
}
#endif

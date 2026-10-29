// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifdef FEATURE_NATIVEAOT
#include "common.h"
#include "gcenv.h"
#include "gcinterface.h"
#include "CommonMacros.inl"
#include "thread.h"
#include "threadstore.h"
#include "threadstore.inl"
#include "thread.inl"

#define FCALL_CONTRACT
#define ALLOC_FCDECL1 FCDECL1
#define ALLOC_FCDECL2 FCDECL2
GPTR_DECL(MethodTable, g_pFreeObjectMethodTable);

#define TRANSITION_ARG_TYPE PInvokeTransitionFrame*
#define RHP_GCALLOC_RETURNS_NULL_ON_FAILURE

static Thread* GetThread()
{
    return ThreadStore::GetCurrentThread();
}

#ifdef HOST_WASM
// This managed helper reuses the caller's shadow stack and never returns to the native allocation frames.
EXTERN_C [[noreturn]] void RhExceptionHandling_FailedAllocation(uintptr_t sp, MethodTable* pMT, bool isOverflow);
#define PREPARE_TRANSITION_ARG() \
    PInvokeTransitionFrame frame = {}; \
    frame.m_ShadowSP = callersStackPointer
#define TRANSITION_ARG_PARAM &frame
#else
#define PREPARE_TRANSITION_ARG() do { } while (0)
#define TRANSITION_ARG_PARAM nullptr
#endif // HOST_WASM
#else
#include <fcall.h>
#include <gcinterface.h>
#include <vars.hpp>
#include <MiscNativeHelpers.h>

#define ALLOC_FCDECL1(...) EXTERN_C FCDECL1(__VA_ARGS__)
#define ALLOC_FCDECL2(...) EXTERN_C FCDECL2(__VA_ARGS__)

struct TransitionBlock;

EXTERN_C void RhExceptionHandling_FailedAllocation_Helper(MethodTable* pMT, bool isOverflow, TransitionBlock* pTransitionBlock);
#define TRANSITION_ARG_TYPE TransitionBlock*
#endif // FEATURE_NATIVEAOT

#ifdef TARGET_WASM
#define CALLER_STACK_POINTER_ARG_DECL , uintptr_t callersStackPointer
#define CALLER_STACK_POINTER_ARG_PASSTHRU , callersStackPointer
#else
#define CALLER_STACK_POINTER_ARG_DECL
#define CALLER_STACK_POINTER_ARG_PASSTHRU
#endif // TARGET_WASM

EXTERN_C Object* RhpGcAlloc(MethodTable* pMT, uint32_t uFlags, uintptr_t numElements, TRANSITION_ARG_TYPE pTransitionArg);

static void FailedAllocation(MethodTable* pMT, bool isOverflow CALLER_STACK_POINTER_ARG_DECL)
{
#ifdef FEATURE_NATIVEAOT
#ifdef HOST_WASM
    RhExceptionHandling_FailedAllocation(callersStackPointer, pMT, isOverflow);
#else
    ASSERT_UNCONDITIONALLY("NYI"); // TODO: Throw overflow or OOM
#endif // HOST_WASM
#else
    PREPARE_TRANSITION_ARG();
    RhExceptionHandling_FailedAllocation_Helper(pMT, isOverflow, TRANSITION_ARG_PARAM);
#endif // FEATURE_NATIVEAOT
}

static Object* AllocateObject(MethodTable* pMT, uint32_t uFlags, intptr_t numElements CALLER_STACK_POINTER_ARG_DECL)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);
    PREPARE_TRANSITION_ARG();
    Object* obj = RhpGcAlloc(pMT, uFlags, numElements, TRANSITION_ARG_PARAM);
#ifdef RHP_GCALLOC_RETURNS_NULL_ON_FAILURE
    if (obj == NULL)
    {
        FailedAllocation(pMT, false /* isOverflow */ CALLER_STACK_POINTER_ARG_PASSTHRU);
    }
#endif // RHP_GCALLOC_RETURNS_NULL_ON_FAILURE

    return obj;
}

ALLOC_FCDECL2(Object*, RhpNewVariableSizeObject, MethodTable* pMT, intptr_t numElements);
FCIMPL2(Object*, RhpNewVariableSizeObject, MethodTable* pMT, intptr_t numElements)
{
    WRAPPER_NO_CONTRACT;
    return AllocateObject(pMT, 0, numElements CALLER_STACK_POINTER_ARG_PASSTHRU);
}
FCIMPLEND

static Object* NewArrayFastCore(MethodTable* pMT, intptr_t size CALLER_STACK_POINTER_ARG_DECL)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);
    if (size < 0 || size > INT32_MAX)
    {
        FailedAllocation(pMT, true /* isOverflow */ CALLER_STACK_POINTER_ARG_PASSTHRU);
    }

    Thread* thread = GetThread();
    ee_alloc_context* cxt = thread->GetEEAllocContext();

    size_t sizeInBytes = (size_t)pMT->GetBaseSize() + ((size_t)size * (size_t)pMT->RawGetComponentSize());
    sizeInBytes = ALIGN_UP(sizeInBytes, sizeof(void*));

    uint8_t* alloc_ptr = cxt->getAllocPtr();
    _ASSERTE(alloc_ptr <= cxt->getCombinedLimit());
    if ((size_t)(cxt->getCombinedLimit() - alloc_ptr) >= sizeInBytes)
    {
        cxt->setAllocPtr(alloc_ptr + sizeInBytes);
        ArrayBase* pObject = (ArrayBase *)alloc_ptr;
        pObject->SetMethodTable(pMT);
        pObject->SetNumComponents((int32_t)size);
        return pObject;
    }

    return AllocateObject(pMT, 0, size CALLER_STACK_POINTER_ARG_PASSTHRU);
}

#if defined(FEATURE_64BIT_ALIGNMENT)
static Object* NewArrayFastAlign8Core(MethodTable* pMT, intptr_t size CALLER_STACK_POINTER_ARG_DECL)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    if (size < 0 || size > INT32_MAX)
    {
        FailedAllocation(pMT, true /* isOverflow */ CALLER_STACK_POINTER_ARG_PASSTHRU);
    }

    Thread* thread = GetThread();
    ee_alloc_context* cxt = thread->GetEEAllocContext();

    size_t sizeInBytes = (size_t)pMT->GetBaseSize() + ((size_t)size * (size_t)pMT->RawGetComponentSize());
    sizeInBytes = ALIGN_UP(sizeInBytes, sizeof(void*));

    uint8_t* alloc_ptr = cxt->getAllocPtr();
    bool requiresPadding = !IS_ALIGNED(alloc_ptr, sizeof(int64_t));
    size_t paddedSize = sizeInBytes;
    if (requiresPadding)
    {
        // We are assuming that allocation of minimal object flips the alignment
        paddedSize += MIN_OBJECT_SIZE;
    }

    _ASSERTE(alloc_ptr <= cxt->getCombinedLimit());
    if ((size_t)(cxt->getCombinedLimit() - alloc_ptr) >= paddedSize)
    {
        cxt->setAllocPtr(alloc_ptr + paddedSize);
        if (requiresPadding)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->SetMethodTable(g_pFreeObjectMethodTable);
            alloc_ptr += MIN_OBJECT_SIZE;
        }
        _ASSERTE(IS_ALIGNED(alloc_ptr, sizeof(int64_t)));
        ArrayBase* pObject = (ArrayBase *)alloc_ptr;
        pObject->SetMethodTable(pMT);
        pObject->SetNumComponents((int32_t)size);
        return pObject;
    }

    return AllocateObject(pMT, GC_ALLOC_ALIGN8, size CALLER_STACK_POINTER_ARG_PASSTHRU);
}

ALLOC_FCDECL2(Object*, RhpNewArrayFastAlign8, MethodTable* pMT, intptr_t size);
FCIMPL2(Object*, RhpNewArrayFastAlign8, MethodTable* pMT, intptr_t size)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    if (size > 0x10000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return AllocateObject(pMT, GC_ALLOC_ALIGN8, size CALLER_STACK_POINTER_ARG_PASSTHRU);
    }

    return NewArrayFastAlign8Core(pMT, size CALLER_STACK_POINTER_ARG_PASSTHRU);
}
FCIMPLEND
#endif // FEATURE_64BIT_ALIGNMENT

ALLOC_FCDECL2(Object*, RhpNewArrayFast, MethodTable* pMT, intptr_t size);
FCIMPL2(Object*, RhpNewArrayFast, MethodTable* pMT, intptr_t size)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

#ifndef HOST_64BIT
    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    if (size > 0x10000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return AllocateObject(pMT, 0, size CALLER_STACK_POINTER_ARG_PASSTHRU);
    }
#endif // !HOST_64BIT

    return NewArrayFastCore(pMT, size CALLER_STACK_POINTER_ARG_PASSTHRU);
}
FCIMPLEND

ALLOC_FCDECL2(Object*, RhpNewPtrArrayFast, MethodTable* pMT, intptr_t size);
FCIMPL2(Object*, RhpNewPtrArrayFast, MethodTable* pMT, intptr_t size)
{
    WRAPPER_NO_CONTRACT;
#ifndef HOST_64BIT
    // if the element count is <= 0x8000000, no overflow is possible because the component size is
    // <= 0x8
    if (size > 0x8000000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return AllocateObject(pMT, 0, size CALLER_STACK_POINTER_ARG_PASSTHRU);
    }
#endif // !HOST_64BIT

    return NewArrayFastCore(pMT, size CALLER_STACK_POINTER_ARG_PASSTHRU);
}
FCIMPLEND

ALLOC_FCDECL1(Object*, RhpNewFast, MethodTable* pMT);
FCIMPL1(Object*, RhpNewFast, MethodTable* pMT)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    Thread* thread = GetThread();
    ee_alloc_context* cxt = thread->GetEEAllocContext();

    size_t sizeInBytes = (size_t)pMT->GetBaseSize();

    uint8_t* alloc_ptr = cxt->getAllocPtr();
    _ASSERTE(alloc_ptr <= cxt->getCombinedLimit());
    if ((size_t)(cxt->getCombinedLimit() - alloc_ptr) >= sizeInBytes)
    {
        cxt->setAllocPtr(alloc_ptr + sizeInBytes);
        ArrayBase* pObject = (ArrayBase*)alloc_ptr;
        pObject->SetMethodTable(pMT);
        return pObject;
    }

    return AllocateObject(pMT, 0, 0 CALLER_STACK_POINTER_ARG_PASSTHRU);
}
FCIMPLEND

#ifdef FEATURE_NATIVEAOT
ALLOC_FCDECL1(Object*, RhpNewFinalizable, MethodTable* pMT);
FCIMPL1(Object*, RhpNewFinalizable, MethodTable* pMT)
{
    _ASSERTE(pMT->HasFinalizer());
    return AllocateObject(pMT, GC_ALLOC_FINALIZE, 0 CALLER_STACK_POINTER_ARG_PASSTHRU);
}
FCIMPLEND
#endif // FEATURE_NATIVEAOT

#if defined(FEATURE_64BIT_ALIGNMENT)
#ifdef FEATURE_NATIVEAOT
ALLOC_FCDECL1(Object*, RhpNewFinalizableAlign8, MethodTable* pMT);
FCIMPL1(Object*, RhpNewFinalizableAlign8, MethodTable* pMT)
{
    return AllocateObject(pMT, GC_ALLOC_FINALIZE | GC_ALLOC_ALIGN8, 0 CALLER_STACK_POINTER_ARG_PASSTHRU);
}
FCIMPLEND
#endif // FEATURE_NATIVEAOT

ALLOC_FCDECL1(Object*, RhpNewFastAlign8, MethodTable* pMT);
FCIMPL1(Object*, RhpNewFastAlign8, MethodTable* pMT)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    Thread* thread = GetThread();
    ee_alloc_context* cxt = thread->GetEEAllocContext();

    size_t sizeInBytes = (size_t)pMT->GetBaseSize();

    uint8_t* alloc_ptr = cxt->getAllocPtr();
    bool requiresPadding = !IS_ALIGNED(alloc_ptr, sizeof(int64_t));
    size_t paddedSize = sizeInBytes;
    if (requiresPadding)
    {
        // We are assuming that allocation of minimal object flips the alignment
        paddedSize += MIN_OBJECT_SIZE;
    }

    _ASSERTE(alloc_ptr <= cxt->getCombinedLimit());
    if ((size_t)(cxt->getCombinedLimit() - alloc_ptr) >= paddedSize)
    {
        cxt->setAllocPtr(alloc_ptr + paddedSize);
        if (requiresPadding)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->SetMethodTable(g_pFreeObjectMethodTable);
            alloc_ptr += MIN_OBJECT_SIZE;
        }
        _ASSERTE(IS_ALIGNED(alloc_ptr, sizeof(int64_t)));
        ArrayBase* pObject = (ArrayBase*)alloc_ptr;
        pObject->SetMethodTable(pMT);
        return pObject;
    }

    return AllocateObject(pMT, GC_ALLOC_ALIGN8, 0 CALLER_STACK_POINTER_ARG_PASSTHRU);
}
FCIMPLEND

ALLOC_FCDECL1(Object*, RhpNewFastMisalign, MethodTable* pMT);
FCIMPL1(Object*, RhpNewFastMisalign, MethodTable* pMT)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    Thread* thread = GetThread();
    ee_alloc_context* cxt = thread->GetEEAllocContext();

    size_t sizeInBytes = (size_t)pMT->GetBaseSize();

    uint8_t* alloc_ptr = cxt->getAllocPtr();
    bool requiresPadding = IS_ALIGNED(alloc_ptr, sizeof(int64_t));
    size_t paddedSize = sizeInBytes;
    if (requiresPadding)
    {
        // We are assuming that allocation of minimal object flips the alignment
        paddedSize += MIN_OBJECT_SIZE;
    }

    _ASSERTE(alloc_ptr <= cxt->getCombinedLimit());
    if ((size_t)(cxt->getCombinedLimit() - alloc_ptr) >= paddedSize)
    {
        cxt->setAllocPtr(alloc_ptr + paddedSize);
        if (requiresPadding)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->SetMethodTable(g_pFreeObjectMethodTable);
            alloc_ptr += MIN_OBJECT_SIZE;
        }
        _ASSERTE((((uint32_t)alloc_ptr) & (sizeof(int64_t) - 1)) == sizeof(int32_t));
        ArrayBase* pObject = (ArrayBase*)alloc_ptr;
        pObject->SetMethodTable(pMT);
        return pObject;
    }

    return AllocateObject(pMT, GC_ALLOC_ALIGN8 | GC_ALLOC_ALIGN8_BIAS, 0 CALLER_STACK_POINTER_ARG_PASSTHRU);
}
FCIMPLEND
#endif // FEATURE_64BIT_ALIGNMENT

#define MAX_STRING_LENGTH 0x3FFFFFDF

ALLOC_FCDECL2(Object*, RhNewString, MethodTable* pMT, intptr_t stringLength);
FCIMPL2(Object*, RhNewString, MethodTable* pMT, intptr_t stringLength)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    if (stringLength > MAX_STRING_LENGTH)
    {
        FailedAllocation(pMT, false CALLER_STACK_POINTER_ARG_PASSTHRU);
    }

    return NewArrayFastCore(pMT, stringLength CALLER_STACK_POINTER_ARG_PASSTHRU);
}
FCIMPLEND

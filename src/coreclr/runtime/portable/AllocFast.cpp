// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <fcall.h>
#include <gcinterface.h>
#include <vars.hpp>
#include <MiscNativeHelpers.h>

struct TransitionBlock;

#ifdef FEATURE_NATIVEAOT
extern void RhExceptionHandling_FailedAllocation(MethodTable *pMT, bool isOverflow);
#define RhExceptionHandling_FailedAllocation_Helper RhExceptionHandling_FailedAllocation
#define TRANSITION_HELPER_ARG_DECL
#define RHP_GCALLOC_RETURNS_NULL_ON_FAILURE 1
#define TRANSITION_ARG_TYPE void*
#define TRANSITION_HELPER_ARG_VALUE nullptr
#define TRANSITION_HELPER_ARG_PREPARED
#define TRANSITION_HELPER_ARG_HELPER_PASSTHRU
#else
EXTERN_C void RhExceptionHandling_FailedAllocation_Helper(MethodTable* pMT, bool isOverflow, TransitionBlock* pTransitionBlock);
#define TRANSITION_ARG_TYPE TransitionBlock*
#ifdef TARGET_WASM
#define TRANSITION_HELPER_ARG_DECL , TRANSITION_ARG_TYPE pTransitionBlock
#define TRANSITION_HELPER_ARG_VALUE pTransitionBlock
#define TRANSITION_HELPER_ARG_HELPER_PASSTHRU , pTransitionBlock
#define TRANSITION_HELPER_ARG_PREPARED , TRANSITION_ARG_PARAM
#else
#define TRANSITION_HELPER_ARG_DECL
#define TRANSITION_HELPER_ARG_VALUE nullptr
#define TRANSITION_HELPER_ARG_PREPARED
#define TRANSITION_HELPER_ARG_HELPER_PASSTHRU
#endif
#endif

EXTERN_C Object* RhpGcAlloc(MethodTable* pMT, uint32_t uFlags, uintptr_t numElements, TRANSITION_ARG_TYPE pTransitionArg);


static Object* AllocateObject(MethodTable* pMT, uint32_t uFlags, INT_PTR numElements TRANSITION_HELPER_ARG_DECL)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);
    Object* obj = RhpGcAlloc(pMT, uFlags, numElements, TRANSITION_HELPER_ARG_VALUE);
#ifdef RHP_GCALLOC_RETURNS_NULL_ON_FAILURE
    if (obj == NULL)
    {
        RhExceptionHandling_FailedAllocation_Helper(pMT, false /* isOverflow */, TRANSITION_HELPER_ARG_VALUE);
    }
#endif

    return obj;
}

EXTERN_C FCDECL2(Object*, RhpNewVariableSizeObject, MethodTable* pMT, INT_PTR numElements);
FCIMPL2(Object*, RhpNewVariableSizeObject, MethodTable* pMT, INT_PTR numElements)
{
    WRAPPER_NO_CONTRACT;
    PREPARE_TRANSITION_ARG();
    return AllocateObject(pMT, 0, numElements TRANSITION_HELPER_ARG_PREPARED);
}
FCIMPLEND

static Object* NewArrayFastCore(MethodTable* pMT, INT_PTR size TRANSITION_HELPER_ARG_DECL)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);
    if (size < 0 || size > INT32_MAX)
    {
        RhExceptionHandling_FailedAllocation_Helper(pMT, true /* isOverflow */, TRANSITION_HELPER_ARG_VALUE);
    }

    Thread* thread = GetThread();
    ee_alloc_context* cxt = thread->GetEEAllocContext();

    size_t sizeInBytes = (size_t)pMT->GetBaseSize() + ((size_t)size * (size_t)pMT->RawGetComponentSize());
    sizeInBytes = ALIGN_UP(sizeInBytes, sizeof(void*));

    uint8_t* alloc_ptr = cxt->getAllocPtr();
    _ASSERTE(alloc_ptr <= cxt->getAllocLimit());
    if ((size_t)(cxt->getAllocLimit() - alloc_ptr) >= sizeInBytes)
    {
        cxt->setAllocPtr(alloc_ptr + sizeInBytes);
        PtrArray* pObject = (PtrArray *)alloc_ptr;
        pObject->SetMethodTable(pMT);
        pObject->SetNumComponents((INT32)size);
        return pObject;
    }

    return AllocateObject(pMT, 0, size TRANSITION_HELPER_ARG_HELPER_PASSTHRU);
}

#if defined(FEATURE_64BIT_ALIGNMENT)
static Object* NewArrayFastAlign8Core(MethodTable* pMT, INT_PTR size TRANSITION_HELPER_ARG_DECL)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    if (size < 0 || size > INT32_MAX)
    {
        RhExceptionHandling_FailedAllocation_Helper(pMT, true /* isOverflow */, TRANSITION_HELPER_ARG_VALUE);
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

    _ASSERTE(alloc_ptr <= cxt->getAllocLimit());
    if ((size_t)(cxt->getAllocLimit() - alloc_ptr) >= paddedSize)
    {
        cxt->setAllocPtr(alloc_ptr + paddedSize);
        if (requiresPadding)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->SetMethodTable(g_pFreeObjectMethodTable);
            alloc_ptr += MIN_OBJECT_SIZE;
        }
        _ASSERTE(IS_ALIGNED(alloc_ptr, sizeof(int64_t)));
        PtrArray* pObject = (PtrArray *)alloc_ptr;
        pObject->SetMethodTable(pMT);
        pObject->SetNumComponents((INT32)size);
        return pObject;
    }

    return AllocateObject(pMT, GC_ALLOC_ALIGN8, size TRANSITION_HELPER_ARG_HELPER_PASSTHRU);
}

EXTERN_C FCDECL2(Object*, RhpNewArrayFastAlign8, MethodTable* pMT, INT_PTR size);
FCIMPL2(Object*, RhpNewArrayFastAlign8, MethodTable* pMT, INT_PTR size)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    PREPARE_TRANSITION_ARG();
    if (size > 0x10000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return AllocateObject(pMT, GC_ALLOC_ALIGN8, size TRANSITION_HELPER_ARG_PREPARED);
    }

    return NewArrayFastAlign8Core(pMT, size TRANSITION_HELPER_ARG_PREPARED);
}
FCIMPLEND
#endif // FEATURE_64BIT_ALIGNMENT

EXTERN_C FCDECL2(Object*, RhpNewArrayFast, MethodTable* pMT, INT_PTR size);
FCIMPL2(Object*, RhpNewArrayFast, MethodTable* pMT, INT_PTR size)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    PREPARE_TRANSITION_ARG();
#ifndef HOST_64BIT
    // if the element count is <= 0x10000, no overflow is possible because the component size is
    // <= 0xffff, and thus the product is <= 0xffff0000, and the base size is only ~12 bytes
    if (size > 0x10000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return AllocateObject(pMT, 0, size TRANSITION_HELPER_ARG_PREPARED);
    }
#endif // !HOST_64BIT

    return NewArrayFastCore(pMT, size TRANSITION_HELPER_ARG_PREPARED);
}
FCIMPLEND

EXTERN_C FCDECL2(Object*, RhpNewPtrArrayFast, MethodTable* pMT, INT_PTR size);
FCIMPL2(Object*, RhpNewPtrArrayFast, MethodTable* pMT, INT_PTR size)
{
    WRAPPER_NO_CONTRACT;
    PREPARE_TRANSITION_ARG();
#ifndef HOST_64BIT
    // if the element count is <= 0x8000000, no overflow is possible because the component size is
    // <= 0x8
    if (size > 0x8000000)
    {
        // Overflow here should result in an OOM. Let the slow path take care of it.
        return AllocateObject(pMT, 0, size TRANSITION_HELPER_ARG_PREPARED);
    }
#endif // !HOST_64BIT

    return NewArrayFastCore(pMT, size TRANSITION_HELPER_ARG_PREPARED);
}
FCIMPLEND

EXTERN_C FCDECL1(Object*, RhpNewFast, MethodTable* pMT);
FCIMPL1(Object*, RhpNewFast, MethodTable* pMT)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    Thread* thread = GetThread();
    ee_alloc_context* cxt = thread->GetEEAllocContext();

    size_t sizeInBytes = (size_t)pMT->GetBaseSize();

    uint8_t* alloc_ptr = cxt->getAllocPtr();
    _ASSERTE(alloc_ptr <= cxt->getAllocLimit());
    if ((size_t)(cxt->getAllocLimit() - alloc_ptr) >= sizeInBytes)
    {
        cxt->setAllocPtr(alloc_ptr + sizeInBytes);
        PtrArray* pObject = (PtrArray*)alloc_ptr;
        pObject->SetMethodTable(pMT);
        return pObject;
    }

    PREPARE_TRANSITION_ARG();
    return AllocateObject(pMT, 0, 0 TRANSITION_HELPER_ARG_PREPARED);
}
FCIMPLEND

#if defined(FEATURE_64BIT_ALIGNMENT)
EXTERN_C FCDECL1(Object*, RhpNewFastAlign8, MethodTable* pMT);
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

    _ASSERTE(alloc_ptr <= cxt->getAllocLimit());
    if ((size_t)(cxt->getAllocLimit() - alloc_ptr) >= paddedSize)
    {
        cxt->setAllocPtr(alloc_ptr + paddedSize);
        if (requiresPadding)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->SetMethodTable(g_pFreeObjectMethodTable);
            alloc_ptr += MIN_OBJECT_SIZE;
        }
        _ASSERTE(IS_ALIGNED(alloc_ptr, sizeof(int64_t)));
        PtrArray* pObject = (PtrArray*)alloc_ptr;
        pObject->SetMethodTable(pMT);
        return pObject;
    }

    PREPARE_TRANSITION_ARG();
    return AllocateObject(pMT, GC_ALLOC_ALIGN8, 0 TRANSITION_HELPER_ARG_PREPARED);
}
FCIMPLEND

EXTERN_C FCDECL1(Object*, RhpNewFastMisalign, MethodTable* pMT);
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

    _ASSERTE(alloc_ptr <= cxt->getAllocLimit());
    if ((size_t)(cxt->getAllocLimit() - alloc_ptr) >= paddedSize)
    {
        cxt->setAllocPtr(alloc_ptr + paddedSize);
        if (requiresPadding)
        {
            Object* dummy = (Object*)alloc_ptr;
            dummy->SetMethodTable(g_pFreeObjectMethodTable);
            alloc_ptr += MIN_OBJECT_SIZE;
        }
        _ASSERTE((((uint32_t)alloc_ptr) & (sizeof(int64_t) - 1)) == sizeof(int32_t));
        PtrArray* pObject = (PtrArray*)alloc_ptr;
        pObject->SetMethodTable(pMT);
        return pObject;
    }

    PREPARE_TRANSITION_ARG();
    return AllocateObject(pMT, GC_ALLOC_ALIGN8 | GC_ALLOC_ALIGN8_BIAS, 0 TRANSITION_HELPER_ARG_PREPARED);
}
FCIMPLEND
#endif // FEATURE_64BIT_ALIGNMENT

#define MAX_STRING_LENGTH 0x3FFFFFDF

EXTERN_C FCDECL2(Object*, RhNewString, MethodTable* pMT, INT_PTR stringLength);
FCIMPL2(Object*, RhNewString, MethodTable* pMT, INT_PTR stringLength)
{
    FCALL_CONTRACT;
    _ASSERTE(pMT != NULL);

    PREPARE_TRANSITION_ARG();

    if (stringLength > MAX_STRING_LENGTH)
    {
        RhExceptionHandling_FailedAllocation_Helper(pMT, false, TRANSITION_ARG_PARAM);
    }

    return NewArrayFastCore(pMT, stringLength TRANSITION_HELPER_ARG_PREPARED);
}
FCIMPLEND

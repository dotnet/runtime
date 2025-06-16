// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "AsmMacros_Shared.h"

// Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
// allocation context then automatically fallback to the slow allocation path.
//  ECX == MethodTable
LEAF_ENTRY RhpNewFast, _TEXT
        // edx = ee_alloc_context pointer, TRASHES eax
        INLINE_GET_ALLOC_CONTEXT_BASE

        mov         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        add         eax, [edx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        jc          LOCAL_LABEL(RhpNewFast_AllocFailed)
        cmp         eax, [edx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        ja          LOCAL_LABEL(RhpNewFast_AllocFailed)
        mov         [edx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        // calc the new object pointer and initialize it
        sub         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx

        ret

LOCAL_LABEL(RhpNewFast_AllocFailed):
        xor         edx, edx
        jmp         RhpNewObject
LEAF_END RhpNewFast, _TEXT

// Allocate non-array object with finalizer.
//  ECX == MethodTable
LEAF_ENTRY RhpNewFinalizable, _TEXT
        mov         edx, GC_ALLOC_FINALIZE                          // Flags
        jmp         RhpNewObject
LEAF_END RhpNewFinalizable, _TEXT

// Allocate non-array object
//  ECX == MethodTable
//  EDX == alloc flags
LEAF_ENTRY RhpNewObject, _TEXT
        PUSH_COOP_PINVOKE_FRAME eax

        // Preserve MethodTable in ESI.
        mov         esi, ecx

        push        eax                                             // transition frame
        push        0                                               // numElements
        push        edx
        push        ecx

        // Call the rest of the allocation helper.
        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call        RhpGcAlloc

        add         esp, 16

        test        eax, eax
        jz          LOCAL_LABEL(NewOutOfMemory)

        POP_COOP_PINVOKE_FRAME

        ret

LOCAL_LABEL(NewOutOfMemory):
        // This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mov         ecx, esi        // Restore MethodTable pointer
        xor         edx, edx        // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME

        jmp         RhExceptionHandling_FailedAllocation
LEAF_END RhpNewObject, _TEXT

// Shared code for RhNewString, RhpNewArrayFast and RhpNewPtrArrayFast
//  EAX == string/array size
//  ECX == MethodTable
//  EDX == character/element count
.macro NEW_ARRAY_FAST_PROLOG
        ESP_PROLOG_BEG
        ESP_PROLOG_PUSH ecx
        ESP_PROLOG_PUSH edx
        ESP_EPILOG_END
.endm

.macro NEW_ARRAY_FAST
        // edx = ee_alloc_context pointer, TRASHES ecx
        INLINE_GET_ALLOC_CONTEXT_BASE

        // ECX == scratch
        // EAX == allocation size
        // EDX == ee_alloc_context pointer

        mov         ecx, eax
        add         eax, [edx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        jc          1f
        cmp         eax, [edx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        ja          1f

        // ECX == allocation size
        // EAX == new alloc ptr
        // EDX == ee_alloc_context pointer

        // set the new alloc pointer
        mov         [edx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        // calc the new object pointer
        sub         eax, ecx

        ESP_EPILOG_BEG
        // Restore the element count and put it in edx
        ESP_EPILOG_POP edx
        // Restore the MethodTable and put it in ecx
        ESP_EPILOG_POP ecx
        ESP_EPILOG_END

        // set the new object's MethodTable pointer and element count
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx
        mov         [eax + OFFSETOF__Array__m_Length], edx
        ret

1:
        ESP_EPILOG_BEG
        // Restore the element count and put it in edx
        ESP_EPILOG_POP edx
        // Restore the MethodTable and put it in ecx
        ESP_EPILOG_POP ecx
        ESP_EPILOG_END

        jmp         RhpNewVariableSizeObject
.endm

// Allocate a new string.
//  ECX == MethodTable
//  EDX == element count
LEAF_ENTRY RhNewString, _TEXT
        // Make sure computing the aligned overall allocation size won't overflow
        cmp         edx, MAX_STRING_LENGTH
        ja          LOCAL_LABEL(RhNewString_StringSizeOverflow)

        // Compute overall allocation size (align(base size + (element size * elements), 4)).
        lea         eax, [(edx * STRING_COMPONENT_SIZE) + (STRING_BASE_SIZE + 3)]
        and         eax, -4

        NEW_ARRAY_FAST_PROLOG
        NEW_ARRAY_FAST

LOCAL_LABEL(RhNewString_StringSizeOverflow):
        // We get here if the size of the final string object can't be represented as an unsigned
        // 32-bit value. We're going to tail-call to a managed helper that will throw
        // an OOM exception that the caller of this allocator understands.

        // ecx holds MethodTable pointer already
        xor         edx, edx            // Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation
LEAF_END RhNewString, _TEXT


// Allocate one dimensional, zero based array (SZARRAY).
//  ECX == MethodTable
//  EDX == element count
LEAF_ENTRY RhpNewArrayFast, _TEXT
        NEW_ARRAY_FAST_PROLOG

        // Compute overall allocation size (align(base size + (element size * elements), 4)).
        // if the element count is <= 0x10000, no overflow is possible because the component size is
        // <= 0xffff, and thus the product is <= 0xffff0000, and the base size for the worst case
        // (32 dimensional MdArray) is less than 0xffff.
        movzx       eax, word ptr [ecx + OFFSETOF__MethodTable__m_usComponentSize]
        cmp         edx, 0x010000
        ja          LOCAL_LABEL(RhpNewArrayFast_ArraySizeBig)
        mul         edx
        lea         eax, [eax + SZARRAY_BASE_SIZE + 3]
LOCAL_LABEL(RhpNewArrayFast_ArrayAlignSize):
        and         eax, -4

        NEW_ARRAY_FAST

LOCAL_LABEL(RhpNewArrayFast_ArraySizeBig):
        // Compute overall allocation size (align(base size + (element size * elements), 4)).
        // if the element count is negative, it's an overflow, otherwise it's out of memory
        cmp         edx, 0
        jl          LOCAL_LABEL(RhpNewArrayFast_ArraySizeOverflow)
        mul         edx
        jc          LOCAL_LABEL(RhpNewArrayFast_ArrayOutOfMemoryNoFrame)
        add         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        jc          LOCAL_LABEL(RhpNewArrayFast_ArrayOutOfMemoryNoFrame)
        add         eax, 3
        jc          LOCAL_LABEL(RhpNewArrayFast_ArrayOutOfMemoryNoFrame)
        jmp         LOCAL_LABEL(RhpNewArrayFast_ArrayAlignSize)

LOCAL_LABEL(RhpNewArrayFast_ArrayOutOfMemoryNoFrame):
        ESP_EPILOG_FREE 8

        // ecx holds MethodTable pointer already
        xor         edx, edx        // Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation

LOCAL_LABEL(RhpNewArrayFast_ArraySizeOverflow):
        ESP_EPILOG_FREE 8

        // We get here if the size of the final array object can't be represented as an unsigned
        // 32-bit value. We're going to tail-call to a managed helper that will throw
        // an overflow exception that the caller of this allocator understands.

        // ecx holds MethodTable pointer already
        mov         edx, 1          // Indicate that we should throw OverflowException
        jmp         RhExceptionHandling_FailedAllocation
LEAF_END RhpNewArrayFast, _TEXT


// Allocate one dimensional, zero based array (SZARRAY) of pointer sized elements.
//  ECX == MethodTable
//  EDX == element count
LEAF_ENTRY RhpNewPtrArrayFast, _TEXT
        // Delegate overflow handling to the generic helper conservatively

        cmp         edx, (0x40000000 / 4) // sizeof(void*)
        jae         RhpNewArrayFast

        // In this case we know the element size is sizeof(void *), or 4 for x86
        // This helps us in two ways - we can shift instead of multiplying, and
        // there's no need to align the size either

        lea         eax, [edx * 4 + SZARRAY_BASE_SIZE]

        NEW_ARRAY_FAST_PROLOG
        NEW_ARRAY_FAST
LEAF_END RhpNewPtrArrayFast, _TEXT

//
// Object* RhpNewVariableSizeObject(MethodTable *pMT, INT_PTR size)
//
// ecx == MethodTable
// edx == element count
//
NESTED_ENTRY RhpNewVariableSizeObject, _TEXT, NoHandler
        PUSH_COOP_PINVOKE_FRAME eax

        // Preserve MethodTable in ESI.
        mov         esi, ecx

        // Push alloc helper arguments (transition frame, size, flags, MethodTable).
        push        eax                                             // transition frame
        push        edx                                             // numElements
        push        0                                               // Flags
        push        ecx                                             // MethodTable

        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call        RhpGcAlloc

        add         esp, 16

        test        eax, eax
        jz          LOCAL_LABEL(RhpNewVariableSizeObject_AllocFailed)

        POP_COOP_PINVOKE_FRAME

        ret

LOCAL_LABEL(RhpNewVariableSizeObject_AllocFailed):
        // This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mov         ecx, esi        // Restore MethodTable pointer
        xor         edx, edx        // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME

        jmp         RhExceptionHandling_FailedAllocation
NESTED_END RhpNewVariableSizeObject, _TEXT

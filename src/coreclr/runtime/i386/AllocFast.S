// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "AsmMacros_Shared.h"

// Shared code for RhpNewFast, RhpNewFastAlign8 and RhpNewFastMisalign
//  ECX == MethodTable
.macro NEW_FAST Variation

        // edx = ee_alloc_context pointer, TRASHES eax
        INLINE_GET_ALLOC_CONTEXT

        // When doing aligned or misaligned allocation we first check
        // the alignment and skip to the regular path if it's already
        // matching the expectation.
        // Otherwise, we try to allocate size + ASM_MIN_OBJECT_SIZE and
        // then prepend a dummy free object at the beginning of the
        // allocation.
.ifnc \Variation,
        mov         eax, [edx + OFFSETOF__ee_alloc_context__alloc_ptr]
        test        eax, 7
.ifc \Variation,Align8
        jz          1f // AlreadyAligned
.else // Variation == "Misalign"
        jnz         1f // AlreadyAligned
.endif

        mov         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        add         eax, ASM_MIN_OBJECT_SIZE
        add         eax, [edx + OFFSETOF__ee_alloc_context__alloc_ptr]
        cmp         eax, [edx + OFFSETOF__ee_alloc_context__combined_limit]
        ja          2f // AllocFailed
        mov         [edx + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        // calc the new object pointer and initialize it
        sub         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx

        // initialize the padding object preceeding the new object
        PREPARE_EXTERNAL_VAR G_FREE_OBJECT_METHOD_TABLE, edx
        mov         [eax + OFFSETOF__Object__m_pEEType - ASM_MIN_OBJECT_SIZE], edx
        mov         dword ptr [eax + OFFSETOF__Array__m_Length - ASM_MIN_OBJECT_SIZE], 0

        ret
.endif // Variation != ""

1: // AlreadyAligned
        mov         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        add         eax, [edx + OFFSETOF__ee_alloc_context__alloc_ptr]
        cmp         eax, [edx + OFFSETOF__ee_alloc_context__combined_limit]
        ja          2f // AllocFailed
        mov         [edx + OFFSETOF__ee_alloc_context__alloc_ptr], eax

        // calc the new object pointer and initialize it
        sub         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        mov         [eax + OFFSETOF__Object__m_pEEType], ecx

        ret

2: // AllocFailed
.ifc \Variation,
        xor         edx, edx
.else
.ifc \Variation,Align8
        mov         edx, GC_ALLOC_ALIGN8
.else
        mov         edx, GC_ALLOC_ALIGN8 + GC_ALLOC_ALIGN8_BIAS
.endif
.endif
        jmp         RhpNewObject

.endm

// Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
// allocation context then automatically fallback to the slow allocation path.
//  ECX == MethodTable
LEAF_ENTRY RhpNewFast, _TEXT
        NEW_FAST
LEAF_END RhpNewFast, _TEXT

// Allocate simple object (not finalizable, array or value type) on an 8 byte boundary.
//  ECX == MethodTable
LEAF_ENTRY RhpNewFastAlign8, _TEXT
        NEW_FAST Align8
LEAF_END RhpNewFastAlign8, _TEXT

// Allocate a value type object (i.e. box it) on an 8 byte boundary + 4 (so that the value type payload
// itself is 8 byte aligned).
//  ECX == MethodTable
LEAF_ENTRY RhpNewFastMisalign, _TEXT
        NEW_FAST Misalign
LEAF_END RhpNewFastMisalign, _TEXT

// Allocate non-array object with finalizer.
//  ECX == MethodTable
LEAF_ENTRY RhpNewFinalizable, _TEXT
        mov         edx, GC_ALLOC_FINALIZE                          // Flags
        jmp         RhpNewObject
LEAF_END RhpNewFinalizable, _TEXT

// Allocate non-array object with finalizer on an 8 byte boundary.
//  ECX == MethodTable
LEAF_ENTRY RhpNewFinalizableAlign8, _TEXT
        mov         edx, GC_ALLOC_FINALIZE + GC_ALLOC_ALIGN8        // Flags
        jmp         RhpNewObject
LEAF_END RhpNewFinalizableAlign8, _TEXT

// Allocate non-array object
//  ECX == MethodTable
//  EDX == alloc flags
LEAF_ENTRY RhpNewObject, _TEXT

        PUSH_COOP_PINVOKE_FRAME eax

        push        eax                                             // transition frame
        push        0                                               // numElements
        push        edx
        push        ecx

        // Call the rest of the allocation helper.
        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call        RhpGcAlloc

        add         esp, 16

        POP_COOP_PINVOKE_FRAME

        test        eax, eax
        jz          NewOutOfMemory

        ret

NewOutOfMemory:
        // This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        xor         edx, edx            // Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation

LEAF_END RhpNewObject, _TEXT

// Shared code for RhNewString, RhpNewArrayFast and RhpNewObjectArray
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
        INLINE_GET_ALLOC_CONTEXT

        // ECX == scratch
        // EAX == allocation size
        // EDX == ee_alloc_context pointer

        mov         ecx, eax
        add         eax, [edx + OFFSETOF__ee_alloc_context__alloc_ptr]
        jc          1f
        cmp         eax, [edx + OFFSETOF__ee_alloc_context__combined_limit]
        ja          1f

        // ECX == allocation size
        // EAX == new alloc ptr
        // EDX == ee_alloc_context pointer

        // set the new alloc pointer
        mov         [edx + OFFSETOF__ee_alloc_context__alloc_ptr], eax

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

        jmp         RhpNewArray

.endm

// Allocate a new string.
//  ECX == MethodTable
//  EDX == element count
LEAF_ENTRY RhNewString, _TEXT

        // Make sure computing the aligned overall allocation size won't overflow
        cmp         edx, MAX_STRING_LENGTH
        ja          StringSizeOverflow

        // Compute overall allocation size (align(base size + (element size * elements), 4)).
        lea         eax, [(edx * STRING_COMPONENT_SIZE) + (STRING_BASE_SIZE + 3)]
        and         eax, -4

        NEW_ARRAY_FAST_PROLOG
        NEW_ARRAY_FAST

StringSizeOverflow:
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
        ja          ArraySizeBig
        mul         edx
        add         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        add         eax, 3
ArrayAlignSize:
        and         eax, -4

        NEW_ARRAY_FAST

ArraySizeBig:
        // Compute overall allocation size (align(base size + (element size * elements), 4)).
        // if the element count is negative, it's an overflow, otherwise it's out of memory
        cmp         edx, 0
        jl          ArraySizeOverflow
        mul         edx
        jc          ArrayOutOfMemoryNoFrame
        add         eax, [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        jc          ArrayOutOfMemoryNoFrame
        add         eax, 3
        jc          ArrayOutOfMemoryNoFrame
        jmp         ArrayAlignSize

ArrayOutOfMemoryNoFrame:
        ESP_EPILOG_FREE 8

        // ecx holds MethodTable pointer already
        xor         edx, edx        // Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation

ArraySizeOverflow:
        ESP_EPILOG_FREE 8

        // We get here if the size of the final array object can't be represented as an unsigned
        // 32-bit value. We're going to tail-call to a managed helper that will throw
        // an overflow exception that the caller of this allocator understands.

        // ecx holds MethodTable pointer already
        mov         edx, 1          // Indicate that we should throw OverflowException
        jmp         RhExceptionHandling_FailedAllocation

LEAF_END RhpNewArrayFast, _TEXT


#ifndef FEATURE_NATIVEAOT
// Allocate one dimensional, zero based array (SZARRAY) of objects (pointer sized elements).
//  ECX == MethodTable
//  EDX == element count
LEAF_ENTRY RhpNewObjectArrayFast, _TEXT

        cmp         edx, (ASM_LARGE_OBJECT_SIZE - 256)/4 // sizeof(void*)
        jae         RhpNewArray

        // In this case we know the element size is sizeof(void *), or 4 for x86
        // This helps us in two ways - we can shift instead of multiplying, and
        // there's no need to align the size either

        mov         eax, dword ptr [ecx + OFFSETOF__MethodTable__m_uBaseSize]
        lea         eax, [eax + edx * 4]

        NEW_ARRAY_FAST_PROLOG
        NEW_ARRAY_FAST

LEAF_END RhpNewObjectArrayFast, _TEXT
#endif

// Shared code for RhpNewArray and RhpNewArrayFastAlign8
.macro NEW_ARRAY Flags
        PUSH_COOP_PINVOKE_FRAME eax

        // Push alloc helper arguments (transition frame, size, flags, MethodTable).
        push        eax                                             // transition frame
        push        edx                                             // numElements
        push        \Flags                                          // Flags
        push        ecx                                             // MethodTable

        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call        RhpGcAlloc

        add         esp, 16

        POP_COOP_PINVOKE_FRAME

        test        eax, eax
        jz          1f

        ret

1:
        // This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        xor         edx, edx        // Indicate that we should throw OOM.
        jmp         RhExceptionHandling_FailedAllocation
.endm

//
// Object* RhpNewArray(MethodTable *pMT, INT_PTR size)
//
// ecx == MethodTable
// edx == element count
//
LEAF_ENTRY RhpNewArray, _TEXT
        NEW_ARRAY 0
LEAF_END RhpNewArray, _TEXT

//
// Object* RhpNewArrayFastAlign8(MethodTable *pMT, INT_PTR size)
//
// ecx == MethodTable
// edx == element count
//
LEAF_ENTRY RhpNewArrayFastAlign8, _TEXT
        // We don't really provide a fast path here. CoreCLR has a configurable threshold
        // for array size to go to large object heap and it's not worth the extra effort
        // to check for it.
        NEW_ARRAY GC_ALLOC_ALIGN8
LEAF_END RhpNewArrayFastAlign8, _TEXT

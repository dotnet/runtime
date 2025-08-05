// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

// Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
// allocation context then automatically fallback to the slow allocation path.
//  x0 == MethodTable
    LEAF_ENTRY RhpNewFast, _TEXT

        // x3 = ee_alloc_context pointer
        INLINE_GET_ALLOC_CONTEXT_BASE x3

        //
        // x0 contains MethodTable pointer
        //
        ldr         w2, [x0, #OFFSETOF__MethodTable__m_uBaseSize]

        //
        // x0: MethodTable pointer
        // x2: base size
        // x3: ee_alloc_context pointer
        //

        // Load potential new object address into x12.
        ldr         x12, [x3, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)]

        // Load and calculate the maximum size of object we can fit.
        ldr         x13, [x3, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit)]
        sub         x13, x13, x12

        // Determine whether the end of the object is too big for the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        cmp         x2, x13
        bhi         LOCAL_LABEL(RhpNewFast_RarePath)

        // Calculate the new alloc pointer to account for the allocation.
        add         x2, x2, x12

        // Set the new object's MethodTable pointer.
        str         x0, [x12, #OFFSETOF__Object__m_pEEType]

        // Update the alloc pointer to the newly calculated one.
        str         x2, [x3, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)]

        mov         x0, x12
        ret

LOCAL_LABEL(RhpNewFast_RarePath):
        mov         x1, #0
        b           C_FUNC(RhpNewObject)

    LEAF_END RhpNewFast, _TEXT


// Allocate non-array object with finalizer.
//  x0 == MethodTable
    LEAF_ENTRY RhpNewFinalizable, _TEXT
        mov         x1, #GC_ALLOC_FINALIZE
        b           C_FUNC(RhpNewObject)
    LEAF_END RhpNewFinalizable, _TEXT


// Allocate non-array object.
//  x0 == MethodTable
//  x1 == alloc flags
    NESTED_ENTRY RhpNewObject, _TEXT, NoHandler

        PUSH_COOP_PINVOKE_FRAME x3

        // x3: transition frame

        // Preserve the MethodTable in x19
        mov         x19, x0

        mov         w2, 0               // numElements

        // Call the rest of the allocation helper.
        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        bl          C_FUNC(RhpGcAlloc)

        // Set the new object's MethodTable pointer on success.
        cbz         x0, LOCAL_LABEL(NewOutOfMemory)

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

        .cfi_restore_state
LOCAL_LABEL(NewOutOfMemory):
        // This is the OOM failure path. We are going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mov         x0, x19            // MethodTable pointer
        mov         x1, 0              // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        b C_FUNC(RhExceptionHandling_FailedAllocation)

    NESTED_END RhpNewObject, _TEXT


// Shared code for RhNewString, RhpNewArrayFast and RhpNewPtrArrayFast
//  x0 == MethodTable
//  x1 == character/element count
//  x2 == string/array size
    .macro NEW_ARRAY_FAST

        INLINE_GET_ALLOC_CONTEXT_BASE x3

        // Load potential new object address into x12.
        ldr         x12, [x3, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)]

        // Load and calculate the maximum size of object we can fit.
        ldr         x13, [x3, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit)]
        sub         x13, x13, x12

        // Determine whether the end of the object is too big for the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        cmp         x2, x13
#if defined(__APPLE__)
        bhi         1f
#else
        bhi         C_FUNC(RhpNewVariableSizeObject)
#endif

        // Calculate the new alloc pointer to account for the allocation.
        add         x2, x2, x12

        // Set the new object's MethodTable pointer and element count.
        str         x0, [x12, #OFFSETOF__Object__m_pEEType]
        str         x1, [x12, #OFFSETOF__Array__m_Length]

        // Update the alloc pointer to the newly calculated one.
        str         x2, [x3, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)]

        // Return the object allocated in x0.
        mov         x0, x12

        ret

#if defined(__APPLE__)
1:
        b           C_FUNC(RhpNewVariableSizeObject)
#endif

    .endm


// Allocate a string.
//  x0 == MethodTable
//  x1 == element/character count
    LEAF_ENTRY RhNewString, _TEXT

        // Make sure computing the overall allocation size wont overflow
        movz        x2, MAX_STRING_LENGTH & 0xFFFF
        movk        x2, MAX_STRING_LENGTH >> 16, lsl 16
        cmp         x1, x2
        bhi         LOCAL_LABEL(StringSizeOverflow)

        // Compute overall allocation size (align(base size + (element size * elements), 8)).
        mov         w2, #STRING_COMPONENT_SIZE
        mov         x3, #(STRING_BASE_SIZE + 7)
        umaddl      x2, w1, w2, x3          // x2 = w1 * w2 + x3
        and         x2, x2, #-8

        NEW_ARRAY_FAST

LOCAL_LABEL(StringSizeOverflow):
        // We get here if the length of the final string object can not be represented as an unsigned
        // 32-bit value. We are going to tail-call to a managed helper that will throw
        // an OOM exception that the caller of this allocator understands.

        // x0 holds MethodTable pointer already
        mov         x1, #1                  // Indicate that we should throw OverflowException
        b           C_FUNC(RhExceptionHandling_FailedAllocation)

    LEAF_END    RhNewString, _Text


// Allocate one dimensional, zero based array (SZARRAY).
//  x0 == MethodTable
//  x1 == element count
    LEAF_ENTRY RhpNewArrayFast, _Text

        // We want to limit the element count to the non-negative 32-bit int range.
        // If the element count is <= 0x7FFFFFFF, no overflow is possible because the component
        // size is <= 0xffff (it is an unsigned 16-bit value), and the base size for the worst
        // case (32 dimensional MdArray) is less than 0xffff, and thus the product fits in 64 bits.
        mov         x2, #0x7FFFFFFF
        cmp         x1, x2
        bhi         LOCAL_LABEL(ArraySizeOverflow)

        ldrh        w2, [x0, #OFFSETOF__MethodTable__m_usComponentSize]
        umull       x2, w1, w2
        add         x2, x2, #(SZARRAY_BASE_SIZE + 7)
        and         x2, x2, #-8

        NEW_ARRAY_FAST

LOCAL_LABEL(ArraySizeOverflow):
        // We get here if the size of the final array object can not be represented as an unsigned
        // 32-bit value. We are going to tail-call to a managed helper that will throw
        // an overflow exception that the caller of this allocator understands.

        // x0 holds MethodTable pointer already
        mov         x1, #1                  // Indicate that we should throw OverflowException
        b           C_FUNC(RhExceptionHandling_FailedAllocation)

    LEAF_END    RhpNewArrayFast, _TEXT


// Allocate one dimensional, zero based array (SZARRAY) of pointer sized elements.
//  x0 == MethodTable
//  x1 == element count
    LEAF_ENTRY RhpNewPtrArrayFast, _Text

        // Delegate overflow handling to the generic helper conservatively

        mov         x2, #(0x40000000 / 8) // sizeof(void*)
        cmp         x1, x2
#if defined(__APPLE__)
        bhs         1f
#else
        bhs         C_FUNC(RhpNewArrayFast)
#endif

        // In this case we know the element size is sizeof(void *), or 8 for arm64
        // This helps us in two ways - we can shift instead of multiplying, and
        // there's no need to align the size either

        lsl         x2, x1, #3
        add         x2, x2, #SZARRAY_BASE_SIZE

        // No need for rounding in this case - element size is 8, and m_BaseSize is guaranteed
        // to be a multiple of 8.

        NEW_ARRAY_FAST

#if defined(__APPLE__)
1:
        b           C_FUNC(RhpNewVariableSizeObject)
#endif

    LEAF_END    RhpNewPtrArrayFast, _TEXT


// Allocate variable sized object (eg. array, string) using the slow path that calls a runtime helper.
//  x0 == MethodTable
//  x1 == element count
    NESTED_ENTRY RhpNewVariableSizeObject, _TEXT, NoHandler

        PUSH_COOP_PINVOKE_FRAME x3

        // Preserve data we will need later into the callee saved registers
        mov         x19, x0             // Preserve MethodTable

        mov         x2, x1              // numElements
        mov         x1, #0              // uFlags

        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        bl          C_FUNC(RhpGcAlloc)

        // Set the new object's MethodTable pointer and length on success.
        cbz         x0, LOCAL_LABEL(RhpNewVariableSizeObject_OutOfMemory)

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

        .cfi_restore_state
LOCAL_LABEL(RhpNewVariableSizeObject_OutOfMemory):
        // This is the OOM failure path. We are going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mov         x0, x19             // MethodTable Pointer
        mov         x1, 0               // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        b C_FUNC(RhExceptionHandling_FailedAllocation)

    NESTED_END RhpNewVariableSizeObject, _TEXT

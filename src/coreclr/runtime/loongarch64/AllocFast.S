// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

// Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
// allocation context then automatically fallback to the slow allocation path.
//  $a0 == MethodTable
    LEAF_ENTRY RhpNewFast, _TEXT

        // a1 = ee_alloc_context pointer
        INLINE_GET_ALLOC_CONTEXT_BASE  $a1

        //
        // a0 contains MethodTable pointer
        //
        ld.w  $a2, $a0, OFFSETOF__ee_alloc_context + OFFSETOF__MethodTable__m_uBaseSize

        //
        // a0: MethodTable pointer
        // a1: ee_alloc_context pointer
        // a2: base size
        //

        // Load potential new object address into t3.
        ld.d  $t3, $a1, OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr

        // Load and calculate the maximum size of object we can fit.
        ld.d  $t2, $a1, OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit
        sub.d  $t2, $t2, $t3

        // Determine whether the end of the object is too big for the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        bltu  $t2, $a2, LOCAL_LABEL(RhpNewFast_RarePath)

        // Calculate the new alloc pointer to account for the allocation.
        add.d  $a2, $a2, $t3

        // Set the new object's MethodTable pointer.
        st.d  $a0, $t3, OFFSETOF__Object__m_pEEType

        // Update the alloc pointer to the newly calculated one.
        st.d  $a2, $a1, OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr

        ori  $a0, $t3, 0
        jirl  $r0, $ra, 0

LOCAL_LABEL(RhpNewFast_RarePath):
        ori  $a1, $zero, 0
        b  RhpNewObject
    LEAF_END RhpNewFast, _TEXT

// Allocate non-array object with finalizer.
//  a0 == MethodTable
    LEAF_ENTRY RhpNewFinalizable, _TEXT
        ori  $a1, $zero, GC_ALLOC_FINALIZE
        b  RhpNewObject
    LEAF_END RhpNewFinalizable, _TEXT

// Allocate non-array object.
//  a0 == MethodTable
//  a1 == alloc flags
    NESTED_ENTRY RhpNewObject, _TEXT, NoHandler

        PUSH_COOP_PINVOKE_FRAME $a3

        // a3: transition frame

        // Preserve the MethodTable in s0
        ori  $s0, $a0, 0

        ori  $a2, $zero, 0 // numElements

        // Call the rest of the allocation helper.
        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        bl  C_FUNC(RhpGcAlloc)

        // Set the new object's MethodTable pointer on success.
        beqz  $a0, LOCAL_LABEL(NewOutOfMemory)

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

        .cfi_restore_state
LOCAL_LABEL(NewOutOfMemory):
        // This is the OOM failure path. We are going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        ori  $a0, $s0, 0                // MethodTable pointer
        ori  $a1, $zero, 0              // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        b C_FUNC(RhExceptionHandling_FailedAllocation)

    NESTED_END RhpNewObject, _TEXT

// Shared code for RhNewString, RhpNewArrayFast and RhpNewPtrArrayFast
//  a0 == MethodTable
//  a1 == character/element count
//  a2 == string/array size
    .macro NEW_ARRAY_FAST

        INLINE_GET_ALLOC_CONTEXT_BASE $a3

        // Load potential new object address into t3.
        ld.d  $t3, $a3, OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr

        // Load and calculate the maximum size of object we can fit.
        ld.d  $t2, $a3, OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit
        sub.d  $t2, $t2, $t3

        // Determine whether the end of the object is too big for the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        bltu  $t2, $a2, C_FUNC(RhpNewVariableSizeObject)

        // Calculate the new alloc pointer to account for the allocation.
        add.d  $a2, $a2, $t3

        // Set the new object's MethodTable pointer and element count.
        st.d  $a0, $t3, OFFSETOF__Object__m_pEEType
        st.d  $a1, $t3, OFFSETOF__Array__m_Length

        // Update the alloc pointer to the newly calculated one.
        st.d  $a2, $a3, OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr

        // Return the object allocated in a0.
        ori  $a0, $t3, 0

        jirl  $r0, $ra, 0

    .endm

// Allocate a string.
//  a0 == MethodTable
//  a1 == element/character count
    LEAF_ENTRY RhNewString, _TEXT

        // Make sure computing the overall allocation size wont overflow
        lu12i.w  $a2, ((MAX_STRING_LENGTH >> 12) & 0xFFFFF)
        ori  $a2, $a2, (MAX_STRING_LENGTH & 0xFFF)
        bltu  $a2, $a1, LOCAL_LABEL(StringSizeOverflow)

        // Compute overall allocation size (align(base size + (element size * elements), 8)).
        ori  $a2, $zero, STRING_COMPONENT_SIZE
        mulw.d.w  $a2, $a1, $a2                 // a2 = (a1[31:0] * a2[31:0])[64:0]
        addi.d  $a2, $a2, STRING_BASE_SIZE + 7  // a2 = a2 + STRING_BASE_SIZE + 7
        bstrins.d  $a2, $r0, 2, 0               // clear the bits[2:0] of $a2

        NEW_ARRAY_FAST

LOCAL_LABEL(StringSizeOverflow):
        // We get here if the length of the final string object can not be represented as an unsigned
        // 32-bit value. We are going to tail-call to a managed helper that will throw
        // an OOM exception that the caller of this allocator understands.

        // a0 holds MethodTable pointer already
        ori  $a1, $zero, 1                  // Indicate that we should throw OverflowException
        b  C_FUNC(RhExceptionHandling_FailedAllocation)

    LEAF_END    RhNewString, _Text

// Allocate one dimensional, zero based array (SZARRAY).
//  $a0 == MethodTable
//  $a1 == element count
    LEAF_ENTRY RhpNewArrayFast, _Text

        // We want to limit the element count to the non-negative 32-bit int range.
        // If the element count is <= 0x7FFFFFFF, no overflow is possible because the component
        // size is <= 0xffff (it is an unsigned 16-bit value), and the base size for the worst
        // case (32 dimensional MdArray) is less than 0xffff, and thus the product fits in 64 bits.
        lu12i.w  $a2, 0x7ffff
        ori  $a2, $a2, 0xfff
        bltu  $a2, $a1, LOCAL_LABEL(ArraySizeOverflow)

        ld.h  $a2, $a0, OFFSETOF__MethodTable__m_usComponentSize
        mulw.d.w  $a2, $a1, $a2
        addi.d  $a2, $a2, SZARRAY_BASE_SIZE + 7
        bstrins.d  $a2, $r0, 2, 0

        NEW_ARRAY_FAST

LOCAL_LABEL(ArraySizeOverflow):
        // We get here if the size of the final array object can not be represented as an unsigned
        // 32-bit value. We are going to tail-call to a managed helper that will throw
        // an overflow exception that the caller of this allocator understands.

        // $a0 holds MethodTable pointer already
        ori  $a1, $zero, 1 // Indicate that we should throw OverflowException
        b  C_FUNC(RhExceptionHandling_FailedAllocation)

    LEAF_END    RhpNewArrayFast, _TEXT

// Allocate one dimensional, zero based array (SZARRAY) of pointer sized elements.
//  $a0 == MethodTable
//  $a1 == element count
    LEAF_ENTRY RhpNewPtrArrayFast, _Text

        // Delegate overflow handling to the generic helper conservatively

        li.w  $a2, (0x40000000 / 8) // sizeof(void*)
        bgeu  $a1, $a2, C_FUNC(RhpNewArrayFast)

        // In this case we know the element size is sizeof(void *), or 8 for arm64
        // This helps us in two ways - we can shift instead of multiplying, and
        // there's no need to align the size either

        slli.d  $a2, $a1, 3
        addi.d  $a2, $a2, SZARRAY_BASE_SIZE

        // No need for rounding in this case - element size is 8, and m_BaseSize is guaranteed
        // to be a multiple of 8.

        NEW_ARRAY_FAST

    LEAF_END    RhpNewPtrArrayFast, _TEXT

// Allocate variable sized object (eg. array, string) using the slow path that calls a runtime helper.
//  a0 == MethodTable
//  a1 == element count
    NESTED_ENTRY RhpNewVariableSizeObject, _TEXT, NoHandler

        PUSH_COOP_PINVOKE_FRAME $a3

        // Preserve data we will need later into the callee saved registers
        ori  $s0, $a0, 0              // Preserve MethodTable

        ori  $a2, $a1, 0              // numElements
        ori  $a1, $zero, 0            // uFlags

        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        bl  C_FUNC(RhpGcAlloc)

        // Set the new object's MethodTable pointer and length on success.
        beqz  $a0, LOCAL_LABEL(RhpNewVariableSizeObject_OutOfMemory)

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

        .cfi_restore_state
LOCAL_LABEL(RhpNewVariableSizeObject_OutOfMemory):
        // This is the OOM failure path. We are going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        ori  $a0, $s0, 0             // MethodTable Pointer
        ori  $a1, $zero, 0           // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        b  C_FUNC(RhExceptionHandling_FailedAllocation)

    NESTED_END RhpNewVariableSizeObject, _TEXT

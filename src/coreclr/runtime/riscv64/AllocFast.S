// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "AsmMacros_Shared.h"

// Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
// allocation context then automatically fallback to the slow allocation path.
//  a0 == MethodTable
    LEAF_ENTRY RhpNewFast, _TEXT
        PROLOG_SAVE_REG_PAIR_INDEXED  fp, ra, 0x20
        PROLOG_SAVE_REG               s1,     0x10

        // Save MethodTable pointer
        mv    s1, a0

        // a0 = ee_alloc_context pointer; trashes volatile registers
        INLINE_GET_ALLOC_CONTEXT_BASE

        //
        // s1 contains MethodTable pointer
        //
        lw    t0, OFFSETOF__MethodTable__m_uBaseSize(s1)

        //
        // s1: MethodTable pointer
        // a0: ee_alloc_context pointer
        // t0: base size
        //

        // Load potential new object address into t1.
        ld    t1, (OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)(a0)

        // Load and calculate the maximum size of object we can fit.
        ld    t2, (OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit)(a0)
        sub   t2, t2, t1

        // Determine whether the end of the object is too big for the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        bltu  t2, t0, LOCAL_LABEL(RhpNewFast_RarePath)

        // Calculate the new alloc pointer to account for the allocation.
        add   t0, t0, t1

        // Set the new object's MethodTable pointer.
        sd    s1, OFFSETOF__Object__m_pEEType(t1)

        // Update the alloc pointer to the newly calculated one.
        sd    t0, (OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)(a0)

        mv    a0, t1

        EPILOG_RESTORE_REG               s1,     0x10
        EPILOG_RESTORE_REG_PAIR_INDEXED  fp, ra, 0x20

        ret

LOCAL_LABEL(RhpNewFast_RarePath):
        mv    a1, zero
        mv    a0, s1

        EPILOG_RESTORE_REG               s1,     0x10
        EPILOG_RESTORE_REG_PAIR_INDEXED  fp, ra, 0x20

        tail  RhpNewObject
    LEAF_END RhpNewFast, _TEXT

// Allocate non-array object with finalizer.
//  a0 == MethodTable
    LEAF_ENTRY RhpNewFinalizable, _TEXT
        li    a1, GC_ALLOC_FINALIZE
        tail  RhpNewObject
    LEAF_END RhpNewFinalizable, _TEXT

// Allocate non-array object.
//  a0 == MethodTable
//  a1 == alloc flags
    NESTED_ENTRY RhpNewObject, _TEXT, NoHandler

        PUSH_COOP_PINVOKE_FRAME a3

        // a3: transition frame

        // Preserve the MethodTable in s2
        mv  s2, a0

        li  a2, 0 // numElements

        // Call the rest of the allocation helper.
        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call  C_FUNC(RhpGcAlloc)

        // Set the new object's MethodTable pointer on success.
        beq  a0, zero, LOCAL_LABEL(NewOutOfMemory)

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

        .cfi_restore_state

LOCAL_LABEL(NewOutOfMemory):
        // This is the OOM failure path. We are going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mv  a0, s2                // MethodTable pointer
        li  a1, 0                 // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        tail  C_FUNC(RhExceptionHandling_FailedAllocation)

    NESTED_END RhpNewObject, _TEXT

// Shared code for RhNewString, RhpNewArrayFast and RhpNewPtrArrayFast
//  a0 == MethodTable
//  a1 == character/element count
//  t0 == string/array size
    .macro NEW_ARRAY_FAST

        PROLOG_SAVE_REG_PAIR_INDEXED  fp, ra, 0x20
        PROLOG_SAVE_REG_PAIR          s1, s2, 0x10

        // Save MethodTable pointer and string length
        mv    s1, a0
        mv    s2, a1

        // a0 = ee_alloc_context pointer; trashes volatile registers
        INLINE_GET_ALLOC_CONTEXT_BASE

        // s1 == MethodTable
        // s2 == element count
        // t0 == string/array size

        // Load potential new object address into t3.
        ld    t1, (OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)(a0)

        // Load and calculate the maximum size of object we can fit.
        ld    t2, (OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit)(a0)
        sub   t2, t2, t1

        // Determine whether the end of the object is too big for the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        bltu  t2, t0, 1f

        // Calculate the new alloc pointer to account for the allocation.
        add   t0, t0, t1

        // Set the new object's MethodTable pointer.
        sd    s1, OFFSETOF__Object__m_pEEType(t1)
        sd    s2, OFFSETOF__Array__m_Length(t1)

        // Update the alloc pointer to the newly calculated one.
        sd    t0, (OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)(a0)

        // Return the object allocated in a0.
        mv    a0, t1

        EPILOG_RESTORE_REG_PAIR          s1, s2, 0x10
        EPILOG_RESTORE_REG_PAIR_INDEXED  fp, ra, 0x20

        ret

1:
        mv    a0, s1
        mv    a1, s2

        EPILOG_RESTORE_REG_PAIR          s1, s2, 0x10
        EPILOG_RESTORE_REG_PAIR_INDEXED  fp, ra, 0x20

        tail  C_FUNC(RhpNewVariableSizeObject)

    .endm

// Allocate a string.
//  a0 == MethodTable
//  a1 == element/character count
    LEAF_ENTRY RhNewString, _TEXT

        // Make sure computing the overall allocation size won't overflow
        li    a2, MAX_STRING_LENGTH
        bltu  a2, a1, LOCAL_LABEL(StringSizeOverflow)   // Branch if a2 < a1 (overflow)

        // Compute overall allocation size (align(base size + (element size * elements), 8)).
        slli  t0, a1, 1                                 // t0 = a1 * STRING_COMPONENT_SIZE, where STRING_COMPONENT_SIZE == 2
        addi  t0, t0, STRING_BASE_SIZE + 7              // t0 = t0 + STRING_BASE_SIZE + 7
        andi  t0, t0, ~0x7                              // Clear the bits[2:0] of t0 (align to 8 bytes)

        NEW_ARRAY_FAST

LOCAL_LABEL(StringSizeOverflow):
        // We get here if the length of the final string object cannot be represented as an unsigned
        // 32-bit value. We are going to tail-call to a managed helper that will throw
        // an OOM exception that the caller of this allocator understands.

        li    a1, 1                  // Indicate that we should throw OverflowException
        tail  C_FUNC(RhExceptionHandling_FailedAllocation)

    LEAF_END RhNewString, _TEXT

// Allocate one-dimensional, zero-based array (SZARRAY).
//  a0 == MethodTable
//  a1 == element count
    LEAF_ENTRY RhpNewArrayFast, _TEXT

        // We want to limit the element count to the non-negative 32-bit int range.
        // If the element count is <= 0x7FFFFFFF, no overflow is possible because the component
        // size is <= 0xffff (it is an unsigned 16-bit value), and the base size for the worst
        // case (32-dimensional MdArray) is less than 0xffff, and thus the product fits in 64 bits.
        li    a2, 0x7fffffff
        bltu  a2, a1, LOCAL_LABEL(ArraySizeOverflow)  // Branch if a2 < a1 (check for overflow)

        lhu   t0, OFFSETOF__MethodTable__m_usComponentSize(a0) // Load component size
        mul   t0, a1, t0                                       // t0 = a1 * component size
        addi  t0, t0, SZARRAY_BASE_SIZE + 7                    // t0 = t0 + 7
        andi  t0, t0, ~0x7                                     // Clear the bits[2:0] of t0 (align to 8 bytes)

        NEW_ARRAY_FAST

LOCAL_LABEL(ArraySizeOverflow):
        // We get here if the size of the final array object cannot be represented as an unsigned
        // 32-bit value. We are going to tail-call to a managed helper that will throw
        // an overflow exception that the caller of this allocator understands.

        li   a1, 1 // Indicate that we should throw OverflowException
        tail  C_FUNC(RhExceptionHandling_FailedAllocation)

    LEAF_END RhpNewArrayFast, _TEXT

// Allocate one dimensional, zero based array (SZARRAY) of pointer sized elements.
//  a0 == MethodTable
//  a1 == element count
    LEAF_ENTRY RhpNewPtrArrayFast, _TEXT

        // Delegate overflow handling to the generic helper conservatively

        li          t0, (0x40000000 / 8) // sizeof(void*)
        bgeu        a1, t0, C_FUNC(RhpNewArrayFast)

        // In this case we know the element size is sizeof(void *), or 8 for arm64
        // This helps us in two ways - we can shift instead of multiplying, and
        // there's no need to align the size either

        sll         t0, a1, 3
        addi        t0, t0, SZARRAY_BASE_SIZE

        // No need for rounding in this case - element size is 8, and m_BaseSize is guaranteed
        // to be a multiple of 8.

        NEW_ARRAY_FAST

    LEAF_END RhpNewPtrArrayFast, _TEXT

// Allocate variable sized object (eg. array, string) using the slow path that calls a runtime helper.
//  a0 == MethodTable
//  a1 == element count
    NESTED_ENTRY RhpNewVariableSizeObject, _TEXT, NoHandler

        PUSH_COOP_PINVOKE_FRAME a3

        // Preserve data we will need later into the callee saved registers
        mv   s2, a0              // Preserve MethodTable

        mv   a2, a1              // numElements
        li   a1, 0                // uFlags

        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call  C_FUNC(RhpGcAlloc)

        // Set the new object's MethodTable pointer and length on success.
        beq  a0, zero, LOCAL_LABEL(RhpNewVariableSizeObject_OutOfMemory)

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

        .cfi_restore_state

LOCAL_LABEL(RhpNewVariableSizeObject_OutOfMemory):
        // This is the OOM failure path. We are going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mv   a0, s2             // MethodTable Pointer
        li   a1, 0              // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        tail  C_FUNC(RhExceptionHandling_FailedAllocation)

    NESTED_END RhpNewVariableSizeObject, _TEXT

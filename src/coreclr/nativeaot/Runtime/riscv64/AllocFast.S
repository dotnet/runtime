// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

// GC type flags
#define GC_ALLOC_FINALIZE               1

//
// Rename fields of nested structs
//
#define OFFSETOF__Thread__m_alloc_context__alloc_ptr        (OFFSETOF__Thread__m_eeAllocContext + OFFSETOF__ee_alloc_context__m_rgbAllocContextBuffer + OFFSETOF__gc_alloc_context__alloc_ptr)
#define OFFSETOF__Thread__m_eeAllocContext__combined_limit  (OFFSETOF__Thread__m_eeAllocContext + OFFSETOF__ee_alloc_context__combined_limit)

// Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
// allocation context then automatically fallback to the slow allocation path.
//  a0 == MethodTable
    LEAF_ENTRY RhpNewFast, _TEXT

        // a1 = GetThread()
        // Save MethodTable pointer. INLINE_GETTHREAD will trash a0.
        mv                t2, a0
        INLINE_GETTHREAD  a1

        //
        // t2 contains MethodTable pointer
        //
        lw  a2, OFFSETOF__MethodTable__m_uBaseSize(t2)

        //
        // t2: MethodTable pointer
        // a1: Thread pointer
        // a2: base size
        //

        // Load potential new object address into t3.
        ld  t3, OFFSETOF__Thread__m_alloc_context__alloc_ptr(a1)

        // Determine whether the end of the object would lie outside of the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        add   a2, a2, t3
        ld    t4, OFFSETOF__Thread__m_eeAllocContext__combined_limit(a1)
        bltu  t4, a2, LOCAL_LABEL(RhpNewFast_RarePath)

        // Update the alloc pointer to account for the allocation.
        sd    a2, OFFSETOF__Thread__m_alloc_context__alloc_ptr(a1)

        // Set the new objects MethodTable pointer
        sd    t2, OFFSETOF__Object__m_pEEType(t3)

        mv    a0, t3
        ret

LOCAL_LABEL(RhpNewFast_RarePath):
        mv    a1, zero
        mv    a0, t2
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

// Allocate a string.
//  a0 == MethodTable
//  a1 == element/character count
    LEAF_ENTRY RhNewString, _TEXT
        // Make sure computing the overall allocation size won't overflow
        li    a2, MAX_STRING_LENGTH
        bltu  a2, a1, LOCAL_LABEL(StringSizeOverflow)   // Branch if a2 < a1 (overflow)

        // Compute overall allocation size (align(base size + (element size * elements), 8)).
        li    a3, STRING_COMPONENT_SIZE                 // Load STRING_COMPONENT_SIZE into a3
        slli  a2, a1, 1                                 // a2 = a1 * STRING_COMPONENT_SIZE, where STRING_COMPONENT_SIZE == 2
        addi  a2, a2, STRING_BASE_SIZE + 7              // a2 = a2 + STRING_BASE_SIZE + 7
        andi  a2, a2, ~0x7                              // Clear the bits[2:0] of a2 (align to 8 bytes)

        // a0 == MethodTable
        // a1 == element count
        // a2 == string size

        // Save MethodTable pointer. INLINE_GETTHREAD will trash a0.
        mv               t2, a0
        INLINE_GETTHREAD a3

        // Load potential new object address into t3.
        ld   t3, OFFSETOF__Thread__m_alloc_context__alloc_ptr(a3)

        // Determine whether the end of the object would lie outside of the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        add  a2, a2, t3
        ld   t3, OFFSETOF__Thread__m_eeAllocContext__combined_limit(a3)
        bltu t3, a2, LOCAL_LABEL(RhNewString_Rare)

        // Reload new object address into t3.
        ld   t3, OFFSETOF__Thread__m_alloc_context__alloc_ptr(a3)

        // Update the alloc pointer to account for the allocation.
        sd   a2, OFFSETOF__Thread__m_alloc_context__alloc_ptr(a3)

        // Set the new object's MethodTable pointer and element count.
        sd   t2, OFFSETOF__Object__m_pEEType(t3)
        sd   a1, OFFSETOF__Array__m_Length(t3)

        // Return the object allocated in a0.
        mv   a0, t3

        ret

LOCAL_LABEL(StringSizeOverflow):
        // We get here if the length of the final string object cannot be represented as an unsigned
        // 32-bit value. We are going to tail-call to a managed helper that will throw
        // an OOM exception that the caller of this allocator understands.

        mv    a0, t2
        li    a1, 1                  // Indicate that we should throw OverflowException
        tail  C_FUNC(RhExceptionHandling_FailedAllocation)

LOCAL_LABEL(RhNewString_Rare):
        mv    a0, t2
        tail  C_FUNC(RhpNewArrayRare)
    LEAF_END RhNewString, _TEXT

// Allocate one-dimensional, zero-based array (SZARRAY).
//  a0 == MethodTable
//  a1 == element count
    LEAF_ENTRY RhpNewArray, _TEXT

        // We want to limit the element count to the non-negative 32-bit int range.
        // If the element count is <= 0x7FFFFFFF, no overflow is possible because the component
        // size is <= 0xffff (it is an unsigned 16-bit value), and the base size for the worst
        // case (32-dimensional MdArray) is less than 0xffff, and thus the product fits in 64 bits.
        li  a2, 0x7fffffff
        bltu  a2, a1, LOCAL_LABEL(ArraySizeOverflow)  // Branch if a2 < a1 (check for overflow)

        lhu  a2, OFFSETOF__MethodTable__m_usComponentSize(a0) // Load component size
        mul  a2, a1, a2                // a2 = a1 * component size
        lw   a3, OFFSETOF__MethodTable__m_uBaseSize(a0)       // Load base size
        add  a2, a2, a3                // a2 = a2 + base size
        addi a2, a2, 7                 // a2 = a2 + 7
        andi a2, a2, ~0x7              // Clear the bits[2:0] of a2 (align to 8 bytes)

        // a0 == MethodTable
        // a1 == element count
        // a2 == array size

        // Save MethodTable pointer. INLINE_GETTHREAD will trash a0.
        mv               t2, a0
        INLINE_GETTHREAD a3

        // Load potential new object address into t3.
        ld   t3, OFFSETOF__Thread__m_alloc_context__alloc_ptr(a3)

        // Determine whether the end of the object would lie outside of the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        add  a2, a2, t3
        ld   t3, OFFSETOF__Thread__m_eeAllocContext__combined_limit(a3)
        bltu t3, a2, LOCAL_LABEL(RhpNewArray_Rare)

        // Reload new object address into t3.
        ld   t3, OFFSETOF__Thread__m_alloc_context__alloc_ptr(a3)

        // Update the alloc pointer to account for the allocation.
        sd   a2, OFFSETOF__Thread__m_alloc_context__alloc_ptr(a3)

        // Set the new object's MethodTable pointer and element count.
        sd   t2, OFFSETOF__Object__m_pEEType(t3)
        sd   a1, OFFSETOF__Array__m_Length(t3)

        // Return the object allocated in a0.
        mv   a0, t3

        ret

LOCAL_LABEL(ArraySizeOverflow):
        // We get here if the size of the final array object cannot be represented as an unsigned
        // 32-bit value. We are going to tail-call to a managed helper that will throw
        // an overflow exception that the caller of this allocator understands.

        mv   a0, t2
        li   a1, 1 // Indicate that we should throw OverflowException
        tail  C_FUNC(RhExceptionHandling_FailedAllocation)

LOCAL_LABEL(RhpNewArray_Rare):
        mv    a0, t2
        tail  C_FUNC(RhpNewArrayRare)
    LEAF_END RhpNewArray, _TEXT

// Allocate one-dimensional, zero-based array (SZARRAY) using the slow path that calls a runtime helper.
//  a0 == MethodTable
//  a1 == element count
//  a2 == array size + Thread::m_alloc_context::alloc_ptr
//  a3 == Thread
    NESTED_ENTRY RhpNewArrayRare, _TEXT, NoHandler

        // Recover array size by subtracting the alloc_ptr from a2.
        ld   t3, OFFSETOF__Thread__m_alloc_context__alloc_ptr(a3)
        sub  a2, a2, t3

        PUSH_COOP_PINVOKE_FRAME a3

        // Preserve data we will need later into the callee saved registers
        mv   s2, a0              // Preserve MethodTable

        mv   a2, a1              // numElements
        li   a1, 0                // uFlags

        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call  C_FUNC(RhpGcAlloc)

        // Set the new object's MethodTable pointer and length on success.
        beq  a0, zero, LOCAL_LABEL(ArrayOutOfMemory)

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

        .cfi_restore_state

LOCAL_LABEL(ArrayOutOfMemory):
        // This is the OOM failure path. We are going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mv   a0, s2             // MethodTable Pointer
        li   a1, 0               // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        tail  C_FUNC(RhExceptionHandling_FailedAllocation)

    NESTED_END RhpNewArrayRare, _TEXT

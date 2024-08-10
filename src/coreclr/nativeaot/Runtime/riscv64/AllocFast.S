// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <unixasmmacros.inc>
#include "AsmOffsets.inc"

// GC type flags
GC_ALLOC_FINALIZE               = 1

//
// Rename fields of nested structs
//
OFFSETOF__Thread__m_alloc_context__alloc_ptr        = OFFSETOF__Thread__m_rgbAllocContextBuffer + OFFSETOF__gc_alloc_context__alloc_ptr
OFFSETOF__Thread__m_alloc_context__alloc_limit      = OFFSETOF__Thread__m_rgbAllocContextBuffer + OFFSETOF__gc_alloc_context__alloc_limit

// Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
// allocation context then automatically fallback to the slow allocation path.
//  $a0 == MethodTable
    LEAF_ENTRY RhpNewFast, _TEXT

        // a1 = GetThread()
#ifdef FEATURE_EMULATED_TLS
        GETTHREAD_ETLS_1
#else
        INLINE_GETTHREAD  $a1
#endif

        //
        // a0 contains MethodTable pointer
        //
        ld   $a2, OFFSETOF__MethodTable__m_uBaseSize($a0)

        //
        // a0: MethodTable pointer
        // a1: Thread pointer
        // a2: base size
        //

        // Load potential new object address into t3.
        ld   $t3, OFFSETOF__Thread__m_alloc_context__alloc_ptr($a1)

        // Determine whether the end of the object would lie outside of the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        add  $a2, $a2, $t3
        ld   $t4, OFFSETOF__Thread__m_alloc_context__alloc_limit($a1)
        bltu $t4, $a2, RhpNewFast_RarePath

        // Update the alloc pointer to account for the allocation.
        sd   $a2, OFFSETOF__Thread__m_alloc_context__alloc_ptr($a1)

        // Set the new objects MethodTable pointer
        sd   $a0, OFFSETOF__Object__m_pEEType($t3)

        mv   $a0, $t3
        j    RhpNewFast_Return

RhpNewFast_RarePath:
        mv   $a1, x0
        j    RhpNewObject

RhpNewFast_Return:
    LEAF_END RhpNewFast, _TEXT

// Allocate non-array object with finalizer.
//  a0 == MethodTable
    LEAF_ENTRY RhpNewFinalizable, _TEXT
        li   $a1, GC_ALLOC_FINALIZE
        j    RhpNewObject
    LEAF_END RhpNewFinalizable, _TEXT

// Allocate non-array object.
//  a0 == MethodTable
//  a1 == alloc flags
    NESTED_ENTRY RhpNewObject, _TEXT, NoHandler

        PUSH_COOP_PINVOKE_FRAME $a3

        // a3: transition frame

        // Preserve the MethodTable in s0
        mv   $s0, $a0

        li   $a2, 0 // numElements

        // Call the rest of the allocation helper.
        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call  C_FUNC(RhpGcAlloc)

        // Set the new object's MethodTable pointer on success.
        beq  $a0, x0, NewOutOfMemory

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

        .cfi_restore_state

NewOutOfMemory:
        // This is the OOM failure path. We are going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mv   $a0, $s0                // MethodTable pointer
        li   $a1, 0                  // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        j    C_FUNC(RhExceptionHandling_FailedAllocation)

    NESTED_END RhpNewObject, _TEXT

// Allocate a string.
//  a0 == MethodTable
//  a1 == element/character count
    LEAF_ENTRY RhNewString, _TEXT
        // Make sure computing the overall allocation size won't overflow
        lui  $a2, (MAX_STRING_LENGTH >> 12) & 0xFFFFF
        ori  $a2, $a2, MAX_STRING_LENGTH & 0xFFF
        bltu $a2, $a1, StringSizeOverflow

        // Compute overall allocation size (align(base size + (element size * elements), 8)).
        li   $a2, STRING_COMPONENT_SIZE
        mul  $a2, $a1, $a2                 // $a2 = (a1 * STRING_COMPONENT_SIZE)
        addi $a2, $a2, STRING_BASE_SIZE + 7  // $a2 = $a2 + STRING_BASE_SIZE + 7
        andi $a2, $a2, ~0x7                // clear the bits[2:0] of $a2

        // a0 == MethodTable
        // a1 == element count
        // a2 == string size

#ifdef FEATURE_EMULATED_TLS
        GETTHREAD_ETLS_3
#else
        INLINE_GETTHREAD $a3
#endif

        // Load potential new object address into t3.
        ld   $t3, OFFSETOF__Thread__m_alloc_context__alloc_ptr($a3)

        // Determine whether the end of the object would lie outside of the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        add  $a2, $a2, $t3
        ld   $t3, OFFSETOF__Thread__m_alloc_context__alloc_limit($a3)
        bltu $t3, $a2, RhNewString_Rare

        // Reload new object address into t3.
        ld   $t3, OFFSETOF__Thread__m_alloc_context__alloc_ptr($a3)

        // Update the alloc pointer to account for the allocation.
        sd   $a2, OFFSETOF__Thread__m_alloc_context__alloc_ptr($a3)

        // Set the new object's MethodTable pointer and element count.
        sd   $a0, OFFSETOF__Object__m_pEEType($t3)
        sd   $a1, OFFSETOF__Array__m_Length($t3)

        // Return the object allocated in $a0.
        mv   $a0, $t3

        j    RhNewString_Return

StringSizeOverflow:
        // We get here if the length of the final string object cannot be represented as an unsigned
        // 32-bit value. We are going to tail-call to a managed helper that will throw
        // an OOM exception that the caller of this allocator understands.

        // a0 holds MethodTable pointer already
        li   $a1, 1                  // Indicate that we should throw OverflowException
        j    C_FUNC(RhExceptionHandling_FailedAllocation)

RhNewString_Rare:
        j    C_FUNC(RhpNewArrayRare)

RhNewString_Return:
    LEAF_END RhNewString, _TEXT

// Allocate one-dimensional, zero-based array (SZARRAY).
//  $a0 == MethodTable
//  $a1 == element count
    LEAF_ENTRY RhpNewArray, _TEXT

        // We want to limit the element count to the non-negative 32-bit int range.
        // If the element count is <= 0x7FFFFFFF, no overflow is possible because the component
        // size is <= 0xffff (it is an unsigned 16-bit value), and the base size for the worst
        // case (32-dimensional MdArray) is less than 0xffff, and thus the product fits in 64 bits.
        lui  $a2, 0x7ffff
        ori  $a2, $a2, 0xfff
        bltu $a2, $a1, ArraySizeOverflow

        ld   $a2, OFFSETOF__MethodTable__m_usComponentSize($a0)
        mul  $a2, $a1, $a2
        ld   $a3, OFFSETOF__MethodTable__m_uBaseSize($a0)
        add  $a2, $a2, $a3
        addi $a2, $a2, 7
        andi $a2, $a2, ~0x7                // clear the bits[2:0] of $a2

        // a0 == MethodTable
        // a1 == element count
        // a2 == array size

        INLINE_GETTHREAD $a3

        // Load potential new object address into t3.
        ld   $t3, OFFSETOF__Thread__m_alloc_context__alloc_ptr($a3)

        // Determine whether the end of the object would lie outside of the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        add  $a2, $a2, $t3
        ld   $t3, OFFSETOF__Thread__m_alloc_context__alloc_limit($a3)
        bltu $t3, $a2, RhpNewArray_Rare

        // Reload new object address into t3.
        ld   $t3, OFFSETOF__Thread__m_alloc_context__alloc_ptr($a3)

        // Update the alloc pointer to account for the allocation.
        sd   $a2, OFFSETOF__Thread__m_alloc_context__alloc_ptr($a3)

        // Set the new object's MethodTable pointer and element count.
        sd   $a0, OFFSETOF__Object__m_pEEType($t3)
        sd   $a1, OFFSETOF__Array__m_Length($t3)

        // Return the object allocated in $a0.
        mv   $a0, $t3

        j    RhpNewArray_Return

ArraySizeOverflow:
        // We get here if the size of the final array object cannot be represented as an unsigned
        // 32-bit value. We are going to tail-call to a managed helper that will throw
        // an overflow exception that the caller of this allocator understands.

        // $a0 holds MethodTable pointer already
        li   $a1, 1 // Indicate that we should throw OverflowException
        j    C_FUNC(RhExceptionHandling_FailedAllocation)

RhpNewArray_Rare:
        j    C_FUNC(RhpNewArrayRare)

RhpNewArray_Return:
    LEAF_END RhpNewArray, _TEXT

// Allocate one-dimensional, zero-based array (SZARRAY) using the slow path that calls a runtime helper.
//  a0 == MethodTable
//  a1 == element count
//  a2 == array size + Thread::m_alloc_context::alloc_ptr
//  a3 == Thread
    NESTED_ENTRY RhpNewArrayRare, _TEXT, NoHandler

        // Recover array size by subtracting the alloc_ptr from a2.
        ld   $t3, OFFSETOF__Thread__m_alloc_context__alloc_ptr($a3)
        sub  $a2, $a2, $t3

        PUSH_COOP_PINVOKE_FRAME $a3

        // Preserve data we will need later into the callee saved registers
        mv   $s0, $a0              // Preserve MethodTable

        mv   $a2, $a1              // numElements
        li   $a1, 0                // uFlags

        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call  C_FUNC(RhpGcAlloc)

        // Set the new object's MethodTable pointer and length on success.
        beq  $a0, x0, ArrayOutOfMemory

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

        .cfi_restore_state

ArrayOutOfMemory:
        // This is the OOM failure path. We are going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mv   $a0, $s0             // MethodTable Pointer
        li   $a1, 0               // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        j    C_FUNC(RhExceptionHandling_FailedAllocation)

    NESTED_END RhpNewArrayRare, _TEXT

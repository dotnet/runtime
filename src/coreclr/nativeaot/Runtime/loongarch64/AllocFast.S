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
        ld.w  $a2, $a0, OFFSETOF__MethodTable__m_uBaseSize

        //
        // a0: MethodTable pointer
        // a1: Thread pointer
        // a2: base size
        //

        // Load potential new object address into t3.
        ld.d  $t3, $a1, OFFSETOF__Thread__m_alloc_context__alloc_ptr

        // Determine whether the end of the object would lie outside of the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        add.d  $a2, $a2, $t3
        ld.d   $t4, $a1, OFFSETOF__Thread__m_alloc_context__alloc_limit
        bltu  $t4, $a2, RhpNewFast_RarePath

        // Update the alloc pointer to account for the allocation.
        st.d  $a2, $a1, OFFSETOF__Thread__m_alloc_context__alloc_ptr

        // Set the new objects MethodTable pointer
        st.d  $a0, $t3, OFFSETOF__Object__m_pEEType

        ori  $a0, $t3, 0
        jirl  $r0, $ra, 0

RhpNewFast_RarePath:
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

        addi.w  $a2, $zero, 0 // numElements

        // Call the rest of the allocation helper.
        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        bl  C_FUNC(RhpGcAlloc)

        // Set the new objects MethodTable pointer on success.
        beq  $a0, $zero, NewOutOfMemory

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

        .cfi_restore_state
NewOutOfMemory:
        // This is the OOM failure path. We are going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        ori  $a0, $s0, 0                // MethodTable pointer
        ori  $a1, $zero, 0              // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        b C_FUNC(RhExceptionHandling_FailedAllocation)

    NESTED_END RhpNewObject, _TEXT

// Allocate a string.
//  a0 == MethodTable
//  a1 == element/character count
    LEAF_ENTRY RhNewString, _TEXT
        // Make sure computing the overall allocation size wont overflow
        lu12i.w  $a2, 0x3ffff       // (MAX_STRING_LENGTH >> 12) & 0xFFFFF
        ori  $a2, $a2, 0xfdf        // MAX_STRING_LENGTH & 0xFFF
        bltu  $a2, $a1, StringSizeOverflow

        // Compute overall allocation size (align(base size + (element size * elements), 8)).
        addi.w  $a2, $zero, STRING_COMPONENT_SIZE
        ori  $a3, $zero, 29         // STRING_BASE_SIZE + 7
        mulw.d.w  $a2, $a1, $a2
        add.d  $a2, $a2, $a3        // a2 = (a1[31:0] * a2[31:0])[64:0] + a3
        srli.d  $a2, $a2, 3
        slli.d  $a2, $a2, 3

        // a0 == MethodTable
        // a1 == element count
        // a2 == string size

#ifdef FEATURE_EMULATED_TLS
        GETTHREAD_ETLS_3
#else
        INLINE_GETTHREAD $a3
#endif

        // Load potential new object address into t3.
        ld.d  $t3, $a3, OFFSETOF__Thread__m_alloc_context__alloc_ptr

        // Determine whether the end of the object would lie outside of the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        add.d  $a2, $a2, $t3
        ld.d  $t3, $a3, OFFSETOF__Thread__m_alloc_context__alloc_limit
        bltu  $t3, $a2, RhNewString_Rare

        // Reload new object address into r12.
        ld.d  $t3, $a3, OFFSETOF__Thread__m_alloc_context__alloc_ptr

        // Update the alloc pointer to account for the allocation.
        st.d  $a2, $a3, OFFSETOF__Thread__m_alloc_context__alloc_ptr

        // Set the new objects MethodTable pointer and element count.
        st.d  $a0, $t3, OFFSETOF__Object__m_pEEType
        st.d  $a1, $t3, OFFSETOF__Array__m_Length

        // Return the object allocated in a0.
        ori  $a0, $t3, 0

        jirl  $r0, $ra, 0

StringSizeOverflow:
        // We get here if the length of the final string object can not be represented as an unsigned
        // 32-bit value. We are going to tail-call to a managed helper that will throw
        // an OOM exception that the caller of this allocator understands.

        // a0 holds MethodTable pointer already
        ori  $a1, $zero, 1                  // Indicate that we should throw OverflowException
        b  C_FUNC(RhExceptionHandling_FailedAllocation)

RhNewString_Rare:
        b  C_FUNC(RhpNewArrayRare)
    LEAF_END    RhNewString, _Text

// Allocate one dimensional, zero based array (SZARRAY).
//  $a0 == MethodTable
//  $a1 == element count
    LEAF_ENTRY RhpNewArray, _Text

        // We want to limit the element count to the non-negative 32-bit int range.
        // If the element count is <= 0x7FFFFFFF, no overflow is possible because the component
        // size is <= 0xffff (it is an unsigned 16-bit value), and the base size for the worst
        // case (32 dimensional MdArray) is less than 0xffff, and thus the product fits in 64 bits.
        lu12i.w  $a2, 0x7ffff
        ori  $a2, $a2, 0xfff
        bltu  $a2, $a1, ArraySizeOverflow

        ld.h  $a2, $a0, OFFSETOF__MethodTable__m_usComponentSize
        mulw.d.w  $a2, $a1, $a2
        ld.w  $a3, $a0, OFFSETOF__MethodTable__m_uBaseSize
        add.d  $a2, $a2, $a3
        addi.d  $a2, $a2, 7
        srli.d  $a2, $a2, 3
        slli.d  $a2, $a2, 3
        // a0 == MethodTable
        // a1 == element count
        // a2 == array size

        INLINE_GETTHREAD $a3

        // Load potential new object address into t3.
        ld.d         $t3, $a3, OFFSETOF__Thread__m_alloc_context__alloc_ptr

        // Determine whether the end of the object would lie outside of the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        add.d  $a2, $a2, $t3
        ld.d  $t3, $a3, OFFSETOF__Thread__m_alloc_context__alloc_limit
        bltu  $t3, $a2, RhpNewArray_Rare

        // Reload new object address into t3.
        ld.d  $t3, $a3, OFFSETOF__Thread__m_alloc_context__alloc_ptr

        // Update the alloc pointer to account for the allocation.
        st.d  $a2, $a3, OFFSETOF__Thread__m_alloc_context__alloc_ptr

        // Set the new objects MethodTable pointer and element count.
        st.d  $a0, $t3, OFFSETOF__Object__m_pEEType
        st.d  $a1, $t3, OFFSETOF__Array__m_Length

        // Return the object allocated in r0.
        ori  $a0, $t3, 0

        jirl  $r0, $ra, 0

ArraySizeOverflow:
        // We get here if the size of the final array object can not be represented as an unsigned
        // 32-bit value. We are going to tail-call to a managed helper that will throw
        // an overflow exception that the caller of this allocator understands.

        // $a0 holds MethodTable pointer already
        ori  $a1, $zero, 1 // Indicate that we should throw OverflowException
        b  C_FUNC(RhExceptionHandling_FailedAllocation)

RhpNewArray_Rare:
        b  C_FUNC(RhpNewArrayRare)
    LEAF_END    RhpNewArray, _TEXT

// Allocate one dimensional, zero based array (SZARRAY) using the slow path that calls a runtime helper.
//  a0 == MethodTable
//  a1 == element count
//  a2 == array size + Thread::m_alloc_context::alloc_ptr
//  a3 == Thread
    NESTED_ENTRY RhpNewArrayRare, _TEXT, NoHandler

        // Recover array size by subtracting the alloc_ptr from a2.
        ld.d  $t3, $a3, OFFSETOF__Thread__m_alloc_context__alloc_ptr
        sub.d  $a2, $a2, $t3

        PUSH_COOP_PINVOKE_FRAME $a3

        // Preserve data we will need later into the callee saved registers
        ori  $s0, $a0, 0              // Preserve MethodTable

        ori  $a2, $a1, 0              // numElements
        ori  $a1, $zero, 0            // uFlags

        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        bl  C_FUNC(RhpGcAlloc)

        // Set the new objects MethodTable pointer and length on success.
        beq  $a0, $zero, ArrayOutOfMemory

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        EPILOG_RETURN

        .cfi_restore_state
ArrayOutOfMemory:
        // This is the OOM failure path. We are going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        ori  $a0, $s0, 0             // MethodTable Pointer
        ori  $a1, $zero, 0           // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        b  C_FUNC(RhExceptionHandling_FailedAllocation)

    NESTED_END RhpNewArrayRare, _TEXT

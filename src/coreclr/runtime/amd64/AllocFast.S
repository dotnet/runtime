// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "AsmMacros_Shared.h"

// Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
// allocation context then automatically fallback to the slow allocation path.
//  RDI == MethodTable
LEAF_ENTRY RhpNewFast, _TEXT

        push_nonvol_reg rbx
        mov         rbx, rdi

        // rax = ee_alloc_context pointer; trashes volatile registers
        INLINE_GET_ALLOC_CONTEXT_BASE

        //
        // rbx contains MethodTable pointer
        //
        mov         edx, [rbx + OFFSETOF__MethodTable__m_uBaseSize]

        //
        // rax: ee_alloc_context pointer
        // rbx: MethodTable pointer
        // rdx: base size
        //

        // Load potential new object address into rsi.
        mov         rsi, [rax + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]

        // Load and calculate the maximum size of object we can fit.ß
        mov         rdi, [rax + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        sub         rdi, rsi

        // Determine whether the end of the object is too big for the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        cmp         rdx, rdi
        ja          LOCAL_LABEL(RhpNewFast_RarePath)

        // Calculate the new alloc pointer to account for the allocation.
        add         rdx, rsi

        // Set the new object's MethodTable pointer.
        mov         [rsi + OFFSETOF__Object__m_pEEType], rbx

        // Update the alloc pointer to the newly calculated one.
        mov         [rax + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], rdx

        mov         rax, rsi

        .cfi_remember_state
        pop_nonvol_reg rbx
        ret

        .cfi_restore_state
        .cfi_def_cfa_offset 16          // workaround cfi_restore_state bug
LOCAL_LABEL(RhpNewFast_RarePath):
        mov         rdi, rbx            // restore MethodTable
        xor         esi, esi
        pop_nonvol_reg rbx
        jmp         C_FUNC(RhpNewObject)

LEAF_END RhpNewFast, _TEXT


// Allocate non-array object with finalizer
//  RDI == MethodTable
LEAF_ENTRY RhpNewFinalizable, _TEXT
        mov         esi, GC_ALLOC_FINALIZE
        jmp         C_FUNC(RhpNewObject)
LEAF_END RhpNewFinalizable, _TEXT


// Allocate non-array object
//  RDI == MethodTable
//  ESI == alloc flags
NESTED_ENTRY RhpNewObject, _TEXT, NoHandler

        PUSH_COOP_PINVOKE_FRAME rcx

        // RCX: transition frame

        // Preserve the MethodTable in RBX
        mov         rbx, rdi

        xor         edx, edx    // numElements

        // Call the rest of the allocation helper.
        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call        C_FUNC(RhpGcAlloc)

        test        rax, rax
        jz          LOCAL_LABEL(NewOutOfMemory)

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        ret

        .cfi_restore_state
        .cfi_def_cfa_offset 96          // workaround cfi_restore_state bug
LOCAL_LABEL(NewOutOfMemory):
        // This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mov         rdi, rbx            // MethodTable pointer
        xor         esi, esi            // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME

        jmp         EXTERNAL_C_FUNC(RhExceptionHandling_FailedAllocation)

NESTED_END RhpNewObject, _TEXT


// Shared code for RhNewString, RhpNewArrayFast and RhpNewPtrArrayFast
//  RAX == string/array size
//  RDI == MethodTable
//  ESI == character/element count
.macro NEW_ARRAY_FAST

        push_nonvol_reg rbx
        push_nonvol_reg r12
        push_nonvol_reg r13

        mov         rbx, rdi    // save MethodTable
        mov         r12, rsi    // save element count
        mov         r13, rax    // save size

        // rax = ee_alloc_context pointer; trashes volatile registers
        INLINE_GET_ALLOC_CONTEXT_BASE

        mov         rcx, rax

        // rcx == ee_alloc_context*
        // rbx == MethodTable
        // r12 == element count
        // r13 == string/array size

        mov         rax, [rcx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]
        mov         rdi, [rcx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit]
        sub         rdi, rax

        // Determine whether the end of the object is too big for the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        cmp         r13, rdi
        ja          1f

        add         r13, rax
        mov         [rax + OFFSETOF__Object__m_pEEType], rbx
        mov         [rax + OFFSETOF__Array__m_Length], r12d
        mov         [rcx + OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r13

        .cfi_remember_state
        pop_nonvol_reg r13
        pop_nonvol_reg r12
        pop_nonvol_reg rbx
        ret

        .cfi_restore_state
        .cfi_def_cfa_offset 32  // workaround cfi_restore_state bug
1:
        mov         rdi, rbx    // restore MethodTable
        mov         rsi, r12    // restore element count

        pop_nonvol_reg r13
        pop_nonvol_reg r12
        pop_nonvol_reg rbx
        jmp C_FUNC(RhpNewVariableSizeObject)

.endm // NEW_ARRAY_FAST


// Allocate a string.
//  RDI == MethodTable
//  ESI == character/element count
LEAF_ENTRY RhNewString, _TEXT

        // we want to limit the element count to the non-negative 32-bit int range
        cmp         rsi, MAX_STRING_LENGTH
        ja          LOCAL_LABEL(StringSizeOverflow)

        // Compute overall allocation size (align(base size + (element size * elements), 8)).
        lea         rax, [rsi * STRING_COMPONENT_SIZE + STRING_BASE_SIZE + 7]
        and         rax, -8

        NEW_ARRAY_FAST

LOCAL_LABEL(StringSizeOverflow):
        // We get here if the size of the final string object can't be represented as an unsigned
        // 32-bit value. We're going to tail-call to a managed helper that will throw
        // an OOM exception that the caller of this allocator understands.

        // rdi holds MethodTable pointer already
        xor         esi, esi            // Indicate that we should throw OOM.
        jmp         EXTERNAL_C_FUNC(RhExceptionHandling_FailedAllocation)

LEAF_END RhNewString, _TEXT


// Allocate one dimensional, zero based array (SZARRAY).
//  RDI == MethodTable
//  ESI == element count
LEAF_ENTRY RhpNewArrayFast, _TEXT

        // we want to limit the element count to the non-negative 32-bit int range
        cmp         rsi, 0x07fffffff
        ja          LOCAL_LABEL(ArraySizeOverflow)

        // Compute overall allocation size (align(base size + (element size * elements), 8)).
        movzx       eax, word ptr [rdi + OFFSETOF__MethodTable__m_usComponentSize]
        imul        rax, rsi
        lea         rax, [rax + SZARRAY_BASE_SIZE + 7]
        and         rax, -8

        NEW_ARRAY_FAST

LOCAL_LABEL(ArraySizeOverflow):
        // We get here if the size of the final array object can't be represented as an unsigned
        // 32-bit value. We're going to tail-call to a managed helper that will throw
        // an overflow exception that the caller of this allocator understands.

        // rdi holds MethodTable pointer already
        mov         esi, 1              // Indicate that we should throw OverflowException
        jmp         EXTERNAL_C_FUNC(RhExceptionHandling_FailedAllocation)

LEAF_END RhpNewArrayFast, _TEXT


// Allocate one dimensional, zero based array (SZARRAY) of pointer sized elements.
//  RDI == MethodTable
//  ESI == element count
LEAF_ENTRY RhpNewPtrArrayFast, _TEXT

        // Delegate overflow handling to the generic helper conservatively

        cmp         rsi, 0x8000000 // (0x40000000 / sizeof(void*))
        jae         C_FUNC(RhpNewArrayFast)

        // In this case we know the element size is sizeof(void *), or 8 for x64
        // This helps us in two ways - we can shift instead of multiplying, and
        // there's no need to align the size either

        lea         eax, [esi * 8 + SZARRAY_BASE_SIZE]

        // No need for rounding in this case - element size is 8, and m_BaseSize is guaranteed
        // to be a multiple of 8.

        NEW_ARRAY_FAST

LEAF_END RhpNewPtrArrayFast, _TEXT


NESTED_ENTRY RhpNewVariableSizeObject, _TEXT, NoHandler

        // rdi == MethodTable
        // rsi == element count

        PUSH_COOP_PINVOKE_FRAME rcx

        // rcx: transition frame

        // Preserve the MethodTable in RBX
        mov         rbx, rdi

        mov         rdx, rsi        // numElements

        // passing MethodTable in rdi
        xor         rsi, rsi        // uFlags
        // passing pTransitionFrame in rcx

        // Call the rest of the allocation helper.
        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        call        C_FUNC(RhpGcAlloc)

        test        rax, rax
        jz          LOCAL_LABEL(RhpNewVariableSizeObject_OutOfMemory)

        .cfi_remember_state
        POP_COOP_PINVOKE_FRAME
        ret

        .cfi_restore_state
        .cfi_def_cfa_offset 96          // workaround cfi_restore_state bug
LOCAL_LABEL(RhpNewVariableSizeObject_OutOfMemory):
        // This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mov         rdi, rbx            // MethodTable pointer
        xor         esi, esi            // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME

        jmp         EXTERNAL_C_FUNC(RhExceptionHandling_FailedAllocation)

NESTED_END RhpNewVariableSizeObject, _TEXT

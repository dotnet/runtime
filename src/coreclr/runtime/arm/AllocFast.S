// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.syntax unified
.thumb

#include "AsmMacros_Shared.h"

// Shared code for RhpNewFast, RhpNewFastAlign8 and RhpNewFastMisalign
//  r0 == MethodTable
.macro NEW_FAST Variation
        PROLOG_PUSH "{r4,lr}"
        mov	    r4, r0 // save MethodTable

        // r0 = ee_alloc_context pointer; trashes volatile registers, expects saved lr
        INLINE_GET_ALLOC_CONTEXT_BASE

        ldr         r2, [r4, #OFFSETOF__MethodTable__m_uBaseSize]

        // Load potential new object address into r3.
        ldr         r3, [r0, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)]

        // Load and calculate the maximum size of object we can fit.
        ldr         r1, [r0, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit)]
        sub         r1, r3

        // When doing aligned or misaligned allocation we first check
        // the alignment and skip to the regular path if it's already
        // matching the expectation.
        // Otherwise, we try to allocate size + ASM_MIN_OBJECT_SIZE and
        // then prepend a dummy free object at the beginning of the
        // allocation.
.ifnc \Variation,
        tst         r3, #0x7
.ifc \Variation,Align8
        beq         1f // AlreadyAligned
.else // Variation == "Misalign"
        bne         1f // AlreadyAligned
.endif

        add         r2, ASM_MIN_OBJECT_SIZE

        // Determine whether the end of the object is too big for the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        cmp         r2, r1
        bhi         2f // AllocFailed

        // Update the alloc pointer to account for the allocation.
        add         r2, r3
        str         r2, [r0, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)]

        // Initialize the padding object preceeding the new object.
        PREPARE_EXTERNAL_VAR_INDIRECT G_FREE_OBJECT_METHOD_TABLE, r2
        str         r2, [r3, #OFFSETOF__Object__m_pEEType]
        mov         r2, #0
        str         r2, [r3, #OFFSETOF__Array__m_Length]

        // Calculate the new object pointer and initialize it.
        add         r3, ASM_MIN_OBJECT_SIZE
        str         r4, [r3, #OFFSETOF__Object__m_pEEType]

        // Return the object allocated in r0.
        mov         r0, r3

        EPILOG_POP  "{r4,pc}"
.endif // Variation != ""

1: // AlreadyAligned

        // r0: ee_alloc_context pointer
        // r1: ee_alloc_context.combined_limit
        // r2: base size
        // r3: ee_alloc_context.alloc_ptr
        // r4: MethodTable pointer

        // Determine whether the end of the object is too big for the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        cmp         r2, r1
        bhi         2f // AllocFailed

        // Calculate the new alloc pointer to account for the allocation.
        add         r2, r3

        // Set the new object's MethodTable pointer.
        str         r4, [r3, #OFFSETOF__Object__m_pEEType]

        // Update the alloc pointer to the newly calculated one.
        str         r2, [r0, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)]

        // Return the object allocated in r0.
        mov         r0, r3

        EPILOG_POP  "{r4,pc}"

2: // AllocFailed
        mov         r0, r4            // restore MethodTable
.ifc \Variation,
        mov         r1, #0
.else
.ifc \Variation,Align8
        mov         r1, #GC_ALLOC_ALIGN8
.else
        mov         r1, #(GC_ALLOC_ALIGN8 | GC_ALLOC_ALIGN8_BIAS)
.endif
.endif
        EPILOG_POP  "{r4,lr}"
        b           C_FUNC(RhpNewObject)
.endm


// Allocate non-array, non-finalizable object. If the allocation doesn't fit into the current thread's
// allocation context then automatically fallback to the slow allocation path.
//  r0 == MethodTable
LEAF_ENTRY RhpNewFast, _TEXT
        NEW_FAST
LEAF_END RhpNewFast, _TEXT


// Allocate simple object (not finalizable, array or value type) on an 8 byte boundary.
//  r0 == MethodTable
LEAF_ENTRY RhpNewFastAlign8, _TEXT
        NEW_FAST Align8
LEAF_END RhpNewFastAlign8, _TEXT


// Allocate a value type object (i.e. box it) on an 8 byte boundary + 4 (so that the value type payload
// itself is 8 byte aligned).
//  r0 == MethodTable
LEAF_ENTRY RhpNewFastMisalign, _TEXT
        NEW_FAST Misalign
LEAF_END RhpNewFastMisalign, _TEXT


// Allocate non-array object with finalizer.
//  r0 == MethodTable
LEAF_ENTRY RhpNewFinalizable, _TEXT
        mov         r1, #GC_ALLOC_FINALIZE
        b           C_FUNC(RhpNewObject)
LEAF_END RhpNewFinalizable, _TEXT


// Allocate a finalizable object (by definition not an array or value type) on an 8 byte boundary.
//  r0 == MethodTable
LEAF_ENTRY RhpNewFinalizableAlign8, _TEXT
        mov         r1, #(GC_ALLOC_FINALIZE | GC_ALLOC_ALIGN8)
        b           C_FUNC(RhpNewObject)
LEAF_END RhpNewFinalizableAlign8, _TEXT


// Allocate non-array object.
//  r0 == MethodTable
//  r1 == alloc flags
NESTED_ENTRY RhpNewObject, _TEXT, NoHandler
        PUSH_COOP_PINVOKE_FRAME r3

        // r0: MethodTable
        // r1: alloc flags
        // r3: transition frame

        // Preserve the MethodTable in r5.
        mov         r5, r0

        mov         r2, #0              // numElements

        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        blx         C_FUNC(RhpGcAlloc)

        cbz         r0, LOCAL_LABEL(NewOutOfMemory)

        POP_COOP_PINVOKE_FRAME
        bx          lr

LOCAL_LABEL(NewOutOfMemory):
        // This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mov         r0, r5            // MethodTable pointer
        mov         r1, #0            // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME

        b           C_FUNC(RhExceptionHandling_FailedAllocation)
NESTED_END RhpNewObject, _TEXT

// Shared code for RhNewString, RhpNewArrayFast and RhpNewObjectArray
//  r0 == MethodTable
//  r1 == character/element count
//  r2 == string/array size
.macro NEW_ARRAY_FAST_PROLOG
        PROLOG_PUSH "{r4-r6,lr}"
.endm

.macro NEW_ARRAY_FAST_TAIL_EPILOG
        EPILOG_POP  "{r4-r6,lr}"
.endm

.macro NEW_ARRAY_FAST
        mov         r4, r0 // Save MethodTable
        mov         r5, r1 // Save element count
        mov         r6, r2 // Save string/array size

        // r0 = ee_alloc_context pointer; trashes volatile registers, expects saved lr
        INLINE_GET_ALLOC_CONTEXT_BASE

        // r4 == MethodTable
        // r5 == element count
        // r6 == string/array size
        // r0 == ee_alloc_context*

        // Load potential new object address into r3.
        ldr         r3, [r0, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)]

        // Load and calculate the maximum size of object we can fit
        ldr         r1, [r0, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__combined_limit)]
        sub         r1, r3

        // Determine whether the end of the object is too big for the current allocation context. If so,
        // we abandon the attempt to allocate the object directly and fall back to the slow helper.
        cmp         r6, r1
        bhi         1f

        // Calculate the alloc pointer to account for the allocation.
        add         r6, r3

        // Set the new object's MethodTable pointer and element count.
        str         r4, [r3, #OFFSETOF__Object__m_pEEType]
        str         r5, [r3, #OFFSETOF__Array__m_Length]

        // Update the alloc pointer to the newly calculated one.
        str         r6, [r0, #(OFFSETOF__ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr)]

        // Return the object allocated in r0.
        mov         r0, r3
        EPILOG_POP  "{r4-r6,pc}"

1:
        mov         r0, r4
        mov         r1, r5
        // r0 == MethodTable
        // r1 == element count
        NEW_ARRAY_FAST_TAIL_EPILOG
        b           C_FUNC(RhpNewVariableSizeObject)
.endm


// Allocate a string.
//  r0 == MethodTable
//  r1 == element/character count
LEAF_ENTRY RhNewString, _TEXT
        NEW_ARRAY_FAST_PROLOG

        // Make sure computing the overall allocation size won't overflow
        MOV32       r12, MAX_STRING_LENGTH
        cmp         r1, r12
        bhi         LOCAL_LABEL(StringSizeOverflow)

        // Compute overall allocation size (align(base size + (element size * elements), 4)).
        mov         r2, #(STRING_BASE_SIZE + 3)
#if STRING_COMPONENT_SIZE == 2
        add         r2, r2, r1, lsl #1                  // r2 += characters * 2
#else
        NotImplementedComponentSize
#endif
        bic         r2, r2, #3

        NEW_ARRAY_FAST

LOCAL_LABEL(StringSizeOverflow):
        // We get here if the size of the final string object can't be represented as an unsigned
        // 32-bit value. We're going to tail-call to a managed helper that will throw
        // an OOM exception that the caller of this allocator understands.

        // MethodTable is in r0 already
        mov         r1, 0                  // Indicate that we should throw OOM
        NEW_ARRAY_FAST_TAIL_EPILOG
        b           C_FUNC(RhExceptionHandling_FailedAllocation)
LEAF_END RhNewString, _TEXT


// Allocate one dimensional, zero based array (SZARRAY).
//  r0 == MethodTable
//  r1 == element count
LEAF_ENTRY RhpNewArrayFast, _TEXT
        NEW_ARRAY_FAST_PROLOG

        // Compute overall allocation size (align(base size + (element size * elements), 4)).
        // if the element count is <= 0x10000, no overflow is possible because the component
        // size is <= 0xffff (it's an unsigned 16-bit value) and thus the product is <= 0xffff0000
        // and the base size for the worst case (32 dimensional MdArray) is less than 0xffff.
        ldrh        r2, [r0, #OFFSETOF__MethodTable__m_usComponentSize]
        cmp         r1, #0x10000
        bhi         LOCAL_LABEL(ArraySizeBig)
        umull       r2, r3, r2, r1
        adds        r2, #(SZARRAY_BASE_SIZE + 3)
LOCAL_LABEL(ArrayAlignSize):
        bic         r2, r2, #3

        NEW_ARRAY_FAST

LOCAL_LABEL(ArraySizeBig):
        // if the element count is negative, it's an overflow error
        cmp         r1, #0
        blt         LOCAL_LABEL(ArraySizeOverflow)

        // now we know the element count is in the signed int range [0..0x7fffffff]
        // overflow in computing the total size of the array size gives an out of memory exception,
        // NOT an overflow exception
        // we already have the component size in r2
        umull       r2, r3, r2, r1
        cbnz        r3, LOCAL_LABEL(ArrayOutOfMemoryFinal)
        ldr         r3, [r0, #OFFSETOF__MethodTable__m_uBaseSize]
        adds        r2, r3
        bcs         LOCAL_LABEL(ArrayOutOfMemoryFinal)
        adds        r2, #3
        bcs         LOCAL_LABEL(ArrayOutOfMemoryFinal)
        b           LOCAL_LABEL(ArrayAlignSize)

LOCAL_LABEL(ArrayOutOfMemoryFinal):

        // MethodTable is in r0 already
        mov         r1, #0                  // Indicate that we should throw OOM.
        NEW_ARRAY_FAST_TAIL_EPILOG
        b           C_FUNC(RhExceptionHandling_FailedAllocation)

LOCAL_LABEL(ArraySizeOverflow):
        // We get here if the size of the final array object can't be represented as an unsigned
        // 32-bit value. We're going to tail-call to a managed helper that will throw
        // an overflow exception that the caller of this allocator understands.

        // MethodTable is in r0 already
        mov         r1, #1                  // Indicate that we should throw OverflowException
        NEW_ARRAY_FAST_TAIL_EPILOG
        b           C_FUNC(RhExceptionHandling_FailedAllocation)
LEAF_END RhpNewArrayFast, _TEXT


// Allocate one dimensional, zero based array (SZARRAY) of pointer sized elements.
//  r0 == MethodTable
//  r1 == element count
LEAF_ENTRY RhpNewPtrArrayFast, _TEXT
        NEW_ARRAY_FAST_PROLOG

        // Delegate overflow handling to the generic helper conservatively

        mov         r2, #1 << 28 // 0x40000000 / sizeof(void*)
        cmp         r1, r2
        bhs         LOCAL_LABEL(RhpNewPtrArrayFast_RarePath)

        mov         r2, #SZARRAY_BASE_SIZE
        add         r2, r2, r1, lsl #2

        NEW_ARRAY_FAST

LOCAL_LABEL(RhpNewPtrArrayFast_RarePath):
        NEW_ARRAY_FAST_TAIL_EPILOG
        b           C_FUNC(RhpNewArrayFast)
LEAF_END RhpNewPtrArrayFast, _TEXT


// Allocate variable sized object (eg. array, string) using the slow path that calls a runtime helper.
//  r0 == MethodTable
//  r1 == element count
NESTED_ENTRY RhpNewVariableSizeObject, _TEXT, NoHandler
        PUSH_COOP_PINVOKE_FRAME r3

        // Preserve the MethodTable in r5.
        mov         r5, r0

        mov         r2, r1          // numElements
        mov         r1, #0          // uFlags

        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        blx         C_FUNC(RhpGcAlloc)

        // Test for failure (NULL return).
        cbz         r0, LOCAL_LABEL(RhpNewVariableSizeObject_OutOfMemory)

        POP_COOP_PINVOKE_FRAME
        bx          lr

LOCAL_LABEL(RhpNewVariableSizeObject_OutOfMemory):

        mov         r0, r5       // MethodTable
        mov         r1, #0       // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME

        b           C_FUNC(RhExceptionHandling_FailedAllocation)

NESTED_END RhpNewVariableSizeObject, _TEXT


// Allocate an array on an 8 byte boundary.
//  r0 == MethodTable
//  r1 == element count
NESTED_ENTRY RhpNewArrayFastAlign8, _TEXT, NoHandler

        PUSH_COOP_PINVOKE_FRAME r3

        // Compute overall allocation size (base size + align((element size * elements), 4)).
        ldrh        r2, [r0, #OFFSETOF__MethodTable__m_usComponentSize]
        umull       r2, r4, r2, r1
        cbnz        r4, LOCAL_LABEL(Array8SizeOverflow)
        adds        r2, #3
        bcs         LOCAL_LABEL(Array8SizeOverflow)
        bic         r2, r2, #3
        ldr         r4, [r0, #OFFSETOF__MethodTable__m_uBaseSize]
        adds        r2, r4
        bcs         LOCAL_LABEL(Array8SizeOverflow)

        // Preserve the MethodTable in r5.
        mov         r5, r0

        mov         r2, r1                  // numElements
        mov         r1, #GC_ALLOC_ALIGN8    // uFlags

        // void* RhpGcAlloc(MethodTable *pEEType, uint32_t uFlags, uintptr_t numElements, void * pTransitionFrame)
        blx         C_FUNC(RhpGcAlloc)

        // Test for failure (NULL return).
        cbz         r0, LOCAL_LABEL(Array8OutOfMemory)

        POP_COOP_PINVOKE_FRAME

        bx          lr

LOCAL_LABEL(Array8SizeOverflow):
        // We get here if the size of the final array object can't be represented as an unsigned
        // 32-bit value. We're going to tail-call to a managed helper that will throw
        // an OOM or overflow exception that the caller of this allocator understands.

        // if the element count is non-negative, it's an OOM error
        cmp         r1, #0
        bge         LOCAL_LABEL(Array8OutOfMemory1)

        // r0 holds MethodTable pointer already
        mov         r1, #1              // Indicate that we should throw OverflowException

        POP_COOP_PINVOKE_FRAME
        b           C_FUNC(RhExceptionHandling_FailedAllocation)

LOCAL_LABEL(Array8OutOfMemory):
        // This is the OOM failure path. We're going to tail-call to a managed helper that will throw
        // an out of memory exception that the caller of this allocator understands.

        mov         r0, r5              // MethodTable pointer

LOCAL_LABEL(Array8OutOfMemory1):

        mov         r1, #0              // Indicate that we should throw OOM.

        POP_COOP_PINVOKE_FRAME
        b           C_FUNC(RhExceptionHandling_FailedAllocation)

NESTED_END RhpNewArrayFastAlign8, _TEXT

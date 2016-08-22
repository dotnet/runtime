; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

;; ==++==
;;

;;
;; ==--==
#include "ksarm.h"

#include "asmconstants.h"

#include "asmmacros.h"

    SETALIAS JIT_Box,?JIT_Box@@YAPAVObject@@PAUCORINFO_CLASS_STRUCT_@@PAX@Z
    SETALIAS JIT_New, ?JIT_New@@YAPAVObject@@PAUCORINFO_CLASS_STRUCT_@@@Z
    SETALIAS JIT_Box, ?JIT_Box@@YAPAVObject@@PAUCORINFO_CLASS_STRUCT_@@PAX@Z
    SETALIAS FramedAllocateString, ?FramedAllocateString@@YAPAVStringObject@@K@Z
    SETALIAS g_pStringClass, ?g_pStringClass@@3PAVMethodTable@@A
    SETALIAS JIT_NewArr1, ?JIT_NewArr1@@YAPAVObject@@PAUCORINFO_CLASS_STRUCT_@@H@Z
    SETALIAS CopyValueClassUnchecked, ?CopyValueClassUnchecked@@YAXPAX0PAVMethodTable@@@Z

    IMPORT $JIT_New
    IMPORT $JIT_Box
    IMPORT $FramedAllocateString
    IMPORT $g_pStringClass
    IMPORT $JIT_NewArr1
    IMPORT $CopyValueClassUnchecked
    IMPORT SetAppDomainInObject


    IMPORT JIT_GetSharedNonGCStaticBase_Helper
    IMPORT JIT_GetSharedGCStaticBase_Helper


    EXPORT JIT_TrialAllocSFastMP_InlineGetThread__PatchTLSOffset
    EXPORT JIT_BoxFastMP_InlineGetThread__PatchTLSOffset
    EXPORT AllocateStringFastMP_InlineGetThread__PatchTLSOffset
    EXPORT JIT_NewArr1VC_MP_InlineGetThread__PatchTLSOffset
    EXPORT JIT_NewArr1OBJ_MP_InlineGetThread__PatchTLSOffset

    EXPORT JIT_GetSharedNonGCStaticBase__PatchTLSLabel
    EXPORT JIT_GetSharedNonGCStaticBaseNoCtor__PatchTLSLabel
    EXPORT JIT_GetSharedGCStaticBase__PatchTLSLabel
    EXPORT JIT_GetSharedGCStaticBaseNoCtor__PatchTLSLabel

    MACRO
    PATCHABLE_INLINE_GETTHREAD $reg, $label
$label
    mrc p15, 0, $reg, c13, c0, 2
    ldr $reg, [$reg, #0xe10]
    MEND


    MACRO
    PATCHABLE_INLINE_GETAPPDOMAIN $reg, $label
$label
    mrc p15, 0, $reg, c13, c0, 2
    ldr $reg, [$reg, #0xe10]
    MEND

    TEXTAREA


    MACRO
    FIX_INDIRECTION $Reg, $label
#ifdef FEATURE_PREJIT
        tst    $Reg, #1
        beq     $label
        ldr    $Reg, [$Reg, #-1]
$label
#endif
    MEND


; ------------------------------------------------------------------
; Start of the writeable code region
    LEAF_ENTRY JIT_PatchedCodeStart
        bx      lr
    LEAF_END

; ------------------------------------------------------------------
; Optimized TLS getters

        ALIGN 4
    LEAF_ENTRY GetThread
        ; This will be overwritten at runtime with optimized GetThread implementation
        b       GetTLSDummy
        ; Just allocate space that will be filled in at runtime
        SPACE (TLS_GETTER_MAX_SIZE_ASM - 2)
    LEAF_END

        ALIGN 4
    LEAF_ENTRY GetAppDomain
        ; This will be overwritten at runtime with optimized GetThread implementation
        b       GetTLSDummy
        ; Just allocate space that will be filled in at runtime
        SPACE (TLS_GETTER_MAX_SIZE_ASM - 2)
    LEAF_END

    LEAF_ENTRY GetTLSDummy
        mov     r0, #0
        bx      lr
    LEAF_END

        ALIGN 4
    LEAF_ENTRY ClrFlsGetBlock
        ; This will be overwritten at runtime with optimized ClrFlsGetBlock implementation
        b       GetTLSDummy
        ; Just allocate space that will be filled in at runtime
        SPACE (TLS_GETTER_MAX_SIZE_ASM - 2)
    LEAF_END

; ------------------------------------------------------------------
; GC write barrier support.
;
; GC Write barriers are defined in asmhelpers.asm. The following functions are used to define 
; patchable location where the write-barriers are copied over at runtime
 
    LEAF_ENTRY JIT_PatchedWriteBarrierStart
        ; Cannot be empty function to prevent LNK1223
        bx lr
    LEAF_END
 
    ; These write barriers are overwritten on the fly
    ; See ValidateWriteBarriers on how the sizes of these should be calculated
        ALIGN 4
    LEAF_ENTRY JIT_WriteBarrier
    SPACE (0x84)
    LEAF_END_MARKED JIT_WriteBarrier

        ALIGN 4
    LEAF_ENTRY JIT_CheckedWriteBarrier
    SPACE (0x9C)
    LEAF_END_MARKED JIT_CheckedWriteBarrier

        ALIGN 4
    LEAF_ENTRY JIT_ByRefWriteBarrier
    SPACE (0xA0)
    LEAF_END_MARKED JIT_ByRefWriteBarrier 

    LEAF_ENTRY JIT_PatchedWriteBarrierLast
        ; Cannot be empty function to prevent LNK1223
        bx lr
    LEAF_END

; JIT Allocation helpers when TLS Index for Thread is low enough for fast helpers

;---------------------------------------------------------------------------
; IN: r0: MethodTable*
;; OUT: r0: new object

    LEAF_ENTRY JIT_TrialAllocSFastMP_InlineGetThread

    ;get object size
    ldr r1, [r0, #MethodTable__m_BaseSize]

    ; m_BaseSize is guaranteed to be a multiple of 4.

    ;getThread
    PATCHABLE_INLINE_GETTHREAD r12, JIT_TrialAllocSFastMP_InlineGetThread__PatchTLSOffset

    ;load current allocation pointers
    ldr r2, [r12, #Thread__m_alloc_context__alloc_limit]
    ldr r3, [r12, #Thread__m_alloc_context__alloc_ptr]

    ;add object size to current pointer 
    add r1, r3

    ;if beyond the limit call c++ method
    cmp r1, r2
    bhi AllocFailed

    ;r1 is the new alloc_ptr and r3 has object address
    ;update the alloc_ptr in Thread
    str r1, [r12, #Thread__m_alloc_context__alloc_ptr]

    ;write methodTable in object
    str r0, [r3]

    ;return object in r0 
    mov r0, r3

#ifdef _DEBUG
    ; Tail call to a helper that will set the current AppDomain index into the object header and then
    ; return the object pointer back to our original caller.
    b       SetAppDomainInObject
#else
    ;return
    bx lr
#endif

AllocFailed
    b       $JIT_New   
    LEAF_END


;---------------------------------------------------------------------------
; HCIMPL2(Object*, JIT_Box, CORINFO_CLASS_HANDLE type, void* unboxedData)
; IN: r0: MethodTable*
; IN: r1: data pointer
;; OUT: r0: new object

    LEAF_ENTRY JIT_BoxFastMP_InlineGetThread

    ldr r2, [r0, #MethodTable__m_pWriteableData]

    ;Check whether the class has been initialized
    ldr r2, [r2, #MethodTableWriteableData__m_dwFlags]
    cmp r2, #MethodTableWriteableData__enum_flag_Unrestored
    bne ClassNotInited

    ; Check whether the object contains pointers
    ldr r3, [r0, #MethodTable__m_dwFlags]
    cmp r3, #MethodTable__enum_flag_ContainsPointers
    bne ContainsPointers

    ldr r2, [r0, #MethodTable__m_BaseSize]

    ;m_BaseSize is guranteed to be a multiple of 4

    ;GetThread
    PATCHABLE_INLINE_GETTHREAD r12, JIT_BoxFastMP_InlineGetThread__PatchTLSOffset

    ldr r3, [r12, #Thread__m_alloc_context__alloc_ptr]
    add r3, r2

    ldr r2, [r12, #Thread__m_alloc_context__alloc_limit]

    cmp r3, r2
    bhi AllocFailed2

    ldr r2, [r12, #Thread__m_alloc_context__alloc_ptr]

    ;advance alloc_ptr in Thread
    str r3, [r12, #Thread__m_alloc_context__alloc_ptr]

    ;write methodtable* in the object
    str r0, [r2]
 
    ;copy the contents of value type in the object

    ldr r3, [r0, #MethodTable__m_BaseSize]
    sub r3, #0xc
   
    ;r3 = no of bytes to copy

    ;move address of object to return register
    mov r0, r2

    ;advance r2 to skip methodtable location
    add r2, #4

CopyLoop
    ldr r12, [r1, r3]
    str r12, [r2, r3]
    sub r3, #4
    bne CopyLoop

#ifdef _DEBUG
    ; Tail call to a helper that will set the current AppDomain index into the object header and then
    ; return the object pointer back to our original caller.
    b       SetAppDomainInObject
#else
    ;return
    bx lr
#endif

ContainsPointers
ClassNotInited
AllocFailed2
    b       $JIT_Box       
    LEAF_END


;---------------------------------------------------------------------------
; IN: r0: number of characters to allocate
;; OUT: r0: address of newly allocated string

    LEAF_ENTRY AllocateStringFastMP_InlineGetThread

    ; Instead of doing elaborate overflow checks, we just limit the number of elements to
    ; MAX_FAST_ALLOCATE_STRING_SIZE. This is picked (in asmconstants.h) to avoid any possibility of
    ; overflow and to ensure we never try to allocate anything here that really should go on the large
    ; object heap instead. Additionally the size has been selected so that it will encode into an
    ; immediate in a single cmp instruction.

    cmp     r0, #MAX_FAST_ALLOCATE_STRING_SIZE
    bhs     OversizedString

    ; Calculate total string size: Align(base size + (characters * 2), 4).
    mov     r1, #(SIZEOF__BaseStringObject + 3) ; r1 == string base size + 3 for alignment round up
    add     r1, r1, r0, lsl #1                  ; r1 += characters * 2
    bic     r1, r1, #3                          ; r1 &= ~3; round size to multiple of 4

    ;GetThread
    PATCHABLE_INLINE_GETTHREAD r12, AllocateStringFastMP_InlineGetThread__PatchTLSOffset
    ldr r2, [r12, #Thread__m_alloc_context__alloc_limit]
    ldr r3, [r12, #Thread__m_alloc_context__alloc_ptr]

    add r1, r3
    cmp r1, r2
    bhi AllocFailed3

    ;can allocate

    ;advance alloc_ptr
    str r1, [r12, #Thread__m_alloc_context__alloc_ptr]

    ; Write MethodTable pointer into new object.
    ldr     r1, =$g_pStringClass
    ldr     r1, [r1]
    str     r1, [r3]

    ; Write string length into new object.
    str     r0, [r3, #StringObject__m_StringLength]

    ;prepare to return new object address
    mov     r0, r3

#ifdef _DEBUG
    ; Tail call to a helper that will set the current AppDomain index into the object header and then
    ; return the object pointer back to our original caller.
    b       SetAppDomainInObject
#else
    ;return
    bx lr
#endif


OversizedString
AllocFailed3
    b       $FramedAllocateString

    LEAF_END


; HCIMPL2(Object*, JIT_NewArr1, CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size)
;---------------------------------------------------------------------------
; IN: r0: type descriptor which contains the (shared) array method table and the element type.
; IN: r1: number of array elements 
;; OUT: r0: address of newly allocated string

    LEAF_ENTRY JIT_NewArr1VC_MP_InlineGetThread

    ; Do a conservative check here for number of elements.  
    ; This is to avoid overflow while doing the calculations.  We don't
    ; have to worry about "large" objects, since the allocation quantum is never big enough for
    ; LARGE_OBJECT_SIZE.

    ; For Value Classes, this needs to be < (max_value_in_4byte - size_of_base_array)/(max_size_of_each_element)
    ; This evaluates to (2^32-1 - 0xc)/2^16

    ; Additionally the constant has been chosen such that it can be encoded in a
    ; single Thumb2 CMP instruction.

    cmp r1, #MAX_FAST_ALLOCATE_ARRAY_VC_SIZE
    bhs OverSizedArray3

    ;load MethodTable from ArrayTypeDesc
    ldr r3, [r0, #ArrayTypeDesc__m_TemplateMT - 2]

    FIX_INDIRECTION r3, label1

    ;get element size - stored in low 16bits of m_dwFlags
    ldrh r12, [r3, #MethodTable__m_dwFlags]

    ; getting size of object to allocate

    ; multiply number of elements with size of each element
    mul r2, r12, r1

    ; add the base array size and 3 to align total bytes at 4 byte boundary
    add r2, r2, #SIZEOF__ArrayOfValueType + 3
    bic r2, #3

    ;GetThread
    PATCHABLE_INLINE_GETTHREAD r12, JIT_NewArr1VC_MP_InlineGetThread__PatchTLSOffset
    ldr r3, [r12, #Thread__m_alloc_context__alloc_ptr]

    add r3, r2

    ldr r2, [r12, #Thread__m_alloc_context__alloc_limit]

    cmp r3, r2
    bhi AllocFailed6

    ; can allocate

    ;r2 = address of new object
    ldr r2, [r12, #Thread__m_alloc_context__alloc_ptr]

    ;update pointer in allocation context
    str r3, [r12, #Thread__m_alloc_context__alloc_ptr]

    ;store number of elements
    str r1, [r2, #ArrayBase__m_NumComponents]

    ;store methodtable
    ldr r3, [r0, #ArrayTypeDesc__m_TemplateMT - 2]

    FIX_INDIRECTION r3, label2

    str r3, [r2]

    ;copy return value
    mov r0, r2

#ifdef _DEBUG
    ; Tail call to a helper that will set the current AppDomain index into the object header and then
    ; return the object pointer back to our original caller.
    b       SetAppDomainInObject
#else
    ;return
    bx lr
#endif



AllocFailed6
OverSizedArray3
    b $JIT_NewArr1

    LEAF_END



; HCIMPL2(Object*, JIT_NewArr1, CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size)
;---------------------------------------------------------------------------
; IN: r0: type descriptor which contains the (shared) array method table and the element type.
; IN: r1: number of array elements 
;; OUT: r0: address of newly allocated string

    LEAF_ENTRY JIT_NewArr1OBJ_MP_InlineGetThread

    cmp r1, #MAX_FAST_ALLOCATE_ARRAY_OBJECTREF_SIZE
    bhs OverSizedArray

    mov r2, #SIZEOF__ArrayOfObjectRef
    add r2, r2, r1, lsl #2
 
    ;r2 will be a multiple of 4
    

    ;GetThread
    PATCHABLE_INLINE_GETTHREAD r12, JIT_NewArr1OBJ_MP_InlineGetThread__PatchTLSOffset
    ldr r3, [r12, #Thread__m_alloc_context__alloc_ptr]

    add r3, r2

    ldr r2, [r12, #Thread__m_alloc_context__alloc_limit]

    cmp r3, r2
    bhi AllocFailed4

    ;can allocate

    ;r2 = address of new object
    ldr r2, [r12, #Thread__m_alloc_context__alloc_ptr]

    ;update pointer in allocation context
    str r3, [r12, #Thread__m_alloc_context__alloc_ptr]

    ;store number of elements
    str r1, [r2, #ArrayBase__m_NumComponents]

    ;store methodtable
    ldr r3, [r0, #ArrayTypeDesc__m_TemplateMT - 2]

    FIX_INDIRECTION r3, label3

    str r3, [r2]

    ;copy return value
    mov r0, r2

#ifdef _DEBUG
    ; Tail call to a helper that will set the current AppDomain index into the object header and then
    ; return the object pointer back to our original caller.
    b       SetAppDomainInObject
#else
    ;return
    bx lr
#endif
    
OverSizedArray
AllocFailed4
    b  $JIT_NewArr1
    LEAF_END

;
; JIT Static access helpers when TLS Index for AppDomain is low enough for fast helpers
;

; ------------------------------------------------------------------
; void* JIT_GetSharedNonGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedNonGCStaticBase_InlineGetAppDomain
    ; Check if r0 (moduleDomainID) is not a moduleID
    tst    r0, #1
    beq      HaveLocalModule1

    PATCHABLE_INLINE_GETAPPDOMAIN r2, JIT_GetSharedNonGCStaticBase__PatchTLSLabel

    ; Get the LocalModule, r0 will always be odd, so: r0 * 2 - 2 <=> (r0 >> 1) * 4
    ldr     r2, [r2 , #AppDomain__m_sDomainLocalBlock + DomainLocalBlock__m_pModuleSlots]
    add     r2, r2, r0, LSL #1
    ldr     r0, [r2, #-2]

HaveLocalModule1
    ; If class is not initialized, bail to C++ helper
    add     r2, r0, #DomainLocalModule__m_pDataBlob
    ldrb    r2, [r2, r1]
    tst     r2, #1
    beq      CallHelper1

    bx      lr

CallHelper1
    ; Tail call JIT_GetSharedNonGCStaticBase_Helper
    b     JIT_GetSharedNonGCStaticBase_Helper
    LEAF_END


; ------------------------------------------------------------------
; void* JIT_GetSharedNonGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedNonGCStaticBaseNoCtor_InlineGetAppDomain
    ; Check if r0 (moduleDomainID) is not a moduleID
    tst    r0, #1
    beq      HaveLocalModule2

    PATCHABLE_INLINE_GETAPPDOMAIN r2, JIT_GetSharedNonGCStaticBaseNoCtor__PatchTLSLabel

    ; Get the LocalModule, r0 will always be odd, so: r0 * 2 - 2 <=> (r0 >> 1) * 4
    ldr     r2, [r2 , #AppDomain__m_sDomainLocalBlock + DomainLocalBlock__m_pModuleSlots]
    add     r2, r2, r0, LSL #1
    ldr     r0, [r2, #-2]


HaveLocalModule2
    bx lr
    LEAF_END


; ------------------------------------------------------------------
; void* JIT_GetSharedGCStaticBase(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedGCStaticBase_InlineGetAppDomain
    ; Check if r0 (moduleDomainID) is not a moduleID
    tst    r0, #1
    beq      HaveLocalModule3

    PATCHABLE_INLINE_GETAPPDOMAIN r2, JIT_GetSharedGCStaticBase__PatchTLSLabel

    ; Get the LocalModule, r0 will always be odd, so: r0 * 2 - 2 <=> (r0 >> 1) * 4
    ldr     r2, [r2 , #AppDomain__m_sDomainLocalBlock + DomainLocalBlock__m_pModuleSlots]
    add     r2, r2, r0, LSL #1
    ldr     r0, [r2, #-2]

HaveLocalModule3
    ; If class is not initialized, bail to C++ helper
    add     r2, r0, #DomainLocalModule__m_pDataBlob
    ldrb    r2, [r2, r1]
    tst     r2, #1
    beq      CallHelper3

    ldr     r0, [r0, #DomainLocalModule__m_pGCStatics]
    bx lr

CallHelper3
    ; Tail call Jit_GetSharedGCStaticBase_Helper
    b     JIT_GetSharedGCStaticBase_Helper
    LEAF_END


; ------------------------------------------------------------------
; void* JIT_GetSharedGCStaticBaseNoCtor(SIZE_T moduleDomainID, DWORD dwClassDomainID)

    LEAF_ENTRY JIT_GetSharedGCStaticBaseNoCtor_InlineGetAppDomain
    ; Check if r0 (moduleDomainID) is not a moduleID
    tst    r0, #1
    beq      HaveLocalModule4

    PATCHABLE_INLINE_GETAPPDOMAIN r2, JIT_GetSharedGCStaticBaseNoCtor__PatchTLSLabel

    ; Get the LocalModule, r0 will always be odd, so: r0 * 2 - 2 <=> (r0 >> 1) * 4
    ldr     r2, [r2 , #AppDomain__m_sDomainLocalBlock + DomainLocalBlock__m_pModuleSlots]
    add     r2, r2, r0, LSL #1
    ldr     r0, [r2, #-2]

HaveLocalModule4
    ldr     r0, [r0, #DomainLocalModule__m_pGCStatics]
    bx lr
    LEAF_END

; ------------------------------------------------------------------
; End of the writeable code region
    LEAF_ENTRY JIT_PatchedCodeLast
        bx      lr
    LEAF_END


; Must be at very end of file 
        END

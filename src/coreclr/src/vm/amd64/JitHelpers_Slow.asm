; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
;

;
; ==--==
; ***********************************************************************
; File: JitHelpers_Slow.asm, see history in jithelp.asm
;
; Notes: These are ASM routinues which we believe to be cold in normal 
;        AMD64 scenarios, mainly because they have other versions which
;        have some more performant nature which will be used in the best
;        cases.
; ***********************************************************************

include AsmMacros.inc
include asmconstants.inc

; Min amount of stack space that a nested function should allocate.
MIN_SIZE equ 28h

EXTERN  g_ephemeral_low:QWORD
EXTERN  g_ephemeral_high:QWORD
EXTERN  g_lowest_address:QWORD
EXTERN  g_highest_address:QWORD
EXTERN  g_card_table:QWORD

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
EXTERN  g_sw_ww_table:QWORD
EXTERN  g_sw_ww_enabled_for_gc_heap:BYTE
endif

ifdef WRITE_BARRIER_CHECK
; Those global variables are always defined, but should be 0 for Server GC
g_GCShadow                      TEXTEQU <?g_GCShadow@@3PEAEEA>
g_GCShadowEnd                   TEXTEQU <?g_GCShadowEnd@@3PEAEEA>
EXTERN  g_GCShadow:QWORD
EXTERN  g_GCShadowEnd:QWORD
endif

JIT_NEW                 equ     ?JIT_New@@YAPEAVObject@@PEAUCORINFO_CLASS_STRUCT_@@@Z
Object__DEBUG_SetAppDomain equ ?DEBUG_SetAppDomain@Object@@QEAAXPEAVAppDomain@@@Z
CopyValueClassUnchecked equ     ?CopyValueClassUnchecked@@YAXPEAX0PEAVMethodTable@@@Z
JIT_Box                 equ     ?JIT_Box@@YAPEAVObject@@PEAUCORINFO_CLASS_STRUCT_@@PEAX@Z
g_pStringClass          equ     ?g_pStringClass@@3PEAVMethodTable@@EA
FramedAllocateString    equ     ?FramedAllocateString@@YAPEAVStringObject@@K@Z
JIT_NewArr1             equ     ?JIT_NewArr1@@YAPEAVObject@@PEAUCORINFO_CLASS_STRUCT_@@_J@Z

INVALIDGCVALUE          equ     0CCCCCCCDh

extern JIT_NEW:proc
extern CopyValueClassUnchecked:proc
extern JIT_Box:proc
extern g_pStringClass:QWORD
extern FramedAllocateString:proc
extern JIT_NewArr1:proc

extern JIT_GetSharedNonGCStaticBase_Helper:proc
extern JIT_GetSharedGCStaticBase_Helper:proc

extern JIT_InternalThrow:proc

ifdef _DEBUG
; Version for when we're sure to be in the GC, checks whether or not the card
; needs to be updated
;
; void JIT_WriteBarrier_Debug(Object** dst, Object* src)
LEAF_ENTRY JIT_WriteBarrier_Debug, _TEXT

ifdef WRITE_BARRIER_CHECK
        ; **ALSO update the shadow GC heap if that is enabled**
        ; Do not perform the work if g_GCShadow is 0
        cmp     g_GCShadow, 0
        je      NoShadow

        ; If we end up outside of the heap don't corrupt random memory
        mov     r10, rcx
        sub     r10, [g_lowest_address]
        jb      NoShadow

        ; Check that our adjusted destination is somewhere in the shadow gc
        add     r10, [g_GCShadow]
        cmp     r10, [g_GCShadowEnd]
        ja      NoShadow

        ; Write ref into real GC; see comment below about possibility of AV
        mov     [rcx], rdx
        ; Write ref into shadow GC
        mov     [r10], rdx

        ; Ensure that the write to the shadow heap occurs before the read from
        ; the GC heap so that race conditions are caught by INVALIDGCVALUE
        mfence

        ; Check that GC/ShadowGC values match
        mov     r11, [rcx]
        mov     rax, [r10]
        cmp     rax, r11
        je      DoneShadow
        mov     r11, INVALIDGCVALUE
        mov     [r10], r11

        jmp     DoneShadow

    ; If we don't have a shadow GC we won't have done the write yet
    NoShadow:
endif

        mov     rax, rdx

        ; Do the move. It is correct to possibly take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rax

ifdef WRITE_BARRIER_CHECK
    ; If we had a shadow GC then we already wrote to the real GC at the same time
    ; as the shadow GC so we want to jump over the real write immediately above
    DoneShadow:
endif

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        ; Update the write watch table if necessary
        cmp     byte ptr [g_sw_ww_enabled_for_gc_heap], 0h
        je      CheckCardTable
        mov     r10, rcx
        shr     r10, 0Ch ; SoftwareWriteWatch::AddressToTableByteIndexShift
        add     r10, qword ptr [g_sw_ww_table]
        cmp     byte ptr [r10], 0h
        jne     CheckCardTable
        mov     byte ptr [r10], 0FFh
endif

    CheckCardTable:
        ; See if we can just quick out
        cmp     rax, [g_ephemeral_low]
        jb      Exit
        cmp     rax, [g_ephemeral_high]
        jnb     Exit

        ; Check if we need to update the card table
        ; Calc pCardByte
        shr     rcx, 0Bh
        add     rcx, [g_card_table]

        ; Check if this card is dirty
        cmp     byte ptr [rcx], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rcx], 0FFh
        ret

    align 16
    Exit:
        REPRET
LEAF_END_MARKED JIT_WriteBarrier_Debug, _TEXT
endif

NESTED_ENTRY JIT_TrialAllocSFastMP, _TEXT
        alloc_stack      MIN_SIZE
        END_PROLOGUE

        CALL_GETTHREAD
        mov     r11, rax

        mov     r8d, [rcx + OFFSET__MethodTable__m_BaseSize]

        ; m_BaseSize is guaranteed to be a multiple of 8.

        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], r8
        mov     [rax], rcx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain
endif ; _DEBUG

        ; epilog
        add     rsp, MIN_SIZE
        ret

    AllocFailed:
        add     rsp, MIN_SIZE
        jmp     JIT_NEW
NESTED_END JIT_TrialAllocSFastMP, _TEXT


; HCIMPL2(Object*, JIT_Box, CORINFO_CLASS_HANDLE type, void* unboxedData)
NESTED_ENTRY JIT_BoxFastMP, _TEXT
        alloc_stack MIN_SIZE
        END_PROLOGUE

        mov     rax, [rcx + OFFSETOF__MethodTable__m_pWriteableData]

        ; Check whether the class has not been initialized
        test    dword ptr [rax + OFFSETOF__MethodTableWriteableData__m_dwFlags], MethodTableWriteableData__enum_flag_Unrestored
        jnz     ClassNotInited

        CALL_GETTHREAD
        mov     r11, rax

        mov     r8d, [rcx + OFFSET__MethodTable__m_BaseSize]

        ; m_BaseSize is guaranteed to be a multiple of 8.

        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], r8
        mov     [rax], rcx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain
endif ; _DEBUG

        ; Check whether the object contains pointers
        test    dword ptr [rcx + OFFSETOF__MethodTable__m_dwFlags], MethodTable__enum_flag_ContainsPointers
        jnz     ContainsPointers

        ; We have no pointers - emit a simple inline copy loop

        mov     ecx, [rcx + OFFSET__MethodTable__m_BaseSize]
        sub     ecx, 18h  ; sizeof(ObjHeader) + sizeof(Object) + last slot

    CopyLoop:
        mov     r8, [rdx+rcx]
        mov     [rax+rcx+8], r8

        sub     ecx, 8
        jge     CopyLoop

        add     rsp, MIN_SIZE
        ret

    ContainsPointers:
        ; Do call to CopyValueClassUnchecked(object, data, pMT)

        mov     [rsp+20h], rax

        mov     r8, rcx
        lea     rcx, [rax + 8]
        call    CopyValueClassUnchecked

        mov     rax, [rsp+20h]

        add     rsp, MIN_SIZE
        ret

    ClassNotInited:
    AllocFailed:
        add     rsp, MIN_SIZE
        jmp     JIT_Box
NESTED_END JIT_BoxFastMP, _TEXT


NESTED_ENTRY AllocateStringFastMP, _TEXT
        alloc_stack MIN_SIZE
        END_PROLOGUE

        ; Instead of doing elaborate overflow checks, we just limit the number of elements
        ; to (LARGE_OBJECT_SIZE - 256)/sizeof(WCHAR) or less.
        ; This will avoid all overflow problems, as well as making sure
        ; big string objects are correctly allocated in the big object heap.

        cmp     ecx, (ASM_LARGE_OBJECT_SIZE - 256)/2
        jae     OversizedString

        CALL_GETTHREAD
        mov     r11, rax

        mov     rdx, [g_pStringClass]
        mov     r8d, [rdx + OFFSET__MethodTable__m_BaseSize]

        ; Calculate the final size to allocate.
        ; We need to calculate baseSize + cnt*2, then round that up by adding 7 and anding ~7.

        lea     r8d, [r8d + ecx*2 + 7]
        and     r8d, -8

        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], r8
        mov     [rax], rdx

        mov     [rax + OFFSETOF__StringObject__m_StringLength], ecx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain
endif ; _DEBUG

        add     rsp, MIN_SIZE
        ret

    OversizedString:
    AllocFailed:
        add     rsp, MIN_SIZE
        jmp     FramedAllocateString
NESTED_END AllocateStringFastMP, _TEXT

FIX_INDIRECTION macro Reg
ifdef FEATURE_PREJIT
        test    Reg, 1
        jz      @F
        mov     Reg, [Reg-1]
    @@:
endif
endm

; HCIMPL2(Object*, JIT_NewArr1, CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size)
NESTED_ENTRY JIT_NewArr1VC_MP, _TEXT
        alloc_stack MIN_SIZE
        END_PROLOGUE

        ; We were passed a type descriptor in RCX, which contains the (shared)
        ; array method table and the element type.

        ; The element count is in RDX

        ; NOTE: if this code is ported for CORINFO_HELP_NEWSFAST_ALIGN8, it will need
        ; to emulate the double-specific behavior of JIT_TrialAlloc::GenAllocArray.

        ; Do a conservative check here.  This is to avoid overflow while doing the calculations.  We don't
        ; have to worry about "large" objects, since the allocation quantum is never big enough for
        ; LARGE_OBJECT_SIZE.

        ; For Value Classes, this needs to be 2^16 - slack (2^32 / max component size), 
        ; The slack includes the size for the array header and round-up ; for alignment.  Use 256 for the
        ; slack value out of laziness.

        ; In both cases we do a final overflow check after adding to the alloc_ptr.

        CALL_GETTHREAD
        mov     r11, rax

        ; we need to load the true method table from the type desc
        mov     r9, [rcx + OFFSETOF__ArrayTypeDesc__m_TemplateMT - 2]
        
        FIX_INDIRECTION r9
 
        cmp     rdx, (65535 - 256)
        jae     OversizedArray

        movzx   r8d, word ptr [r9 + OFFSETOF__MethodTable__m_dwFlags]  ; component size is low 16 bits
        imul    r8d, edx  ; signed mul, but won't overflow due to length restriction above
        add     r8d, dword ptr [r9 + OFFSET__MethodTable__m_BaseSize]

        ; round the size to a multiple of 8

        add     r8d, 7
        and     r8d, -8

        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     r8, rax
        jc      AllocFailed

        cmp     r8, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], r8
        mov     [rax], r9

        mov     dword ptr [rax + OFFSETOF__ArrayBase__m_NumComponents], edx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain
endif ; _DEBUG

        add     rsp, MIN_SIZE
        ret

    OversizedArray:
    AllocFailed:
        add     rsp, MIN_SIZE
        jmp     JIT_NewArr1
NESTED_END JIT_NewArr1VC_MP, _TEXT


; HCIMPL2(Object*, JIT_NewArr1, CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size)
NESTED_ENTRY JIT_NewArr1OBJ_MP, _TEXT
        alloc_stack MIN_SIZE
        END_PROLOGUE

        ; We were passed a type descriptor in RCX, which contains the (shared)
        ; array method table and the element type.

        ; The element count is in RDX

        ; NOTE: if this code is ported for CORINFO_HELP_NEWSFAST_ALIGN8, it will need
        ; to emulate the double-specific behavior of JIT_TrialAlloc::GenAllocArray.

        ; Verifies that LARGE_OBJECT_SIZE fits in 32-bit.  This allows us to do array size
        ; arithmetic using 32-bit registers.
        .erre ASM_LARGE_OBJECT_SIZE lt 100000000h

        cmp     rdx, (ASM_LARGE_OBJECT_SIZE - 256)/8
        jae     OversizedArray

        CALL_GETTHREAD
        mov     r11, rax

        ; we need to load the true method table from the type desc
        mov     r9, [rcx + OFFSETOF__ArrayTypeDesc__m_TemplateMT - 2]

        FIX_INDIRECTION r9
 
        ; In this case we know the element size is sizeof(void *), or 8 for x64
        ; This helps us in two ways - we can shift instead of multiplying, and
        ; there's no need to align the size either

        mov     r8d, dword ptr [r9 + OFFSET__MethodTable__m_BaseSize]
        lea     r8d, [r8d + edx * 8]

        ; No need for rounding in this case - element size is 8, and m_BaseSize is guaranteed
        ; to be a multiple of 8.

        mov     r10, [r11 + OFFSET__Thread__m_alloc_context__alloc_limit]
        mov     rax, [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr]

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     [r11 + OFFSET__Thread__m_alloc_context__alloc_ptr], r8
        mov     [rax], r9

        mov     dword ptr [rax + OFFSETOF__ArrayBase__m_NumComponents], edx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain
endif ; _DEBUG

        add     rsp, MIN_SIZE
        ret

    OversizedArray:
    AllocFailed:
        add     rsp, MIN_SIZE
        jmp     JIT_NewArr1
NESTED_END JIT_NewArr1OBJ_MP, _TEXT




extern g_global_alloc_lock:dword
extern g_global_alloc_context:qword

LEAF_ENTRY JIT_TrialAllocSFastSP, _TEXT

        mov     r8d, [rcx + OFFSET__MethodTable__m_BaseSize]

        ; m_BaseSize is guaranteed to be a multiple of 8.

        inc     [g_global_alloc_lock]
        jnz     JIT_NEW

        mov     rax, [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_ptr]       ; alloc_ptr
        mov     r10, [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_limit]     ; limit_ptr

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     qword ptr [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_ptr], r8     ; update the alloc ptr
        mov     [rax], rcx
        mov     [g_global_alloc_lock], -1

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain_NoScratchArea
endif ; _DEBUG

        ret

    AllocFailed:
        mov     [g_global_alloc_lock], -1
        jmp     JIT_NEW
LEAF_END JIT_TrialAllocSFastSP, _TEXT

; HCIMPL2(Object*, JIT_Box, CORINFO_CLASS_HANDLE type, void* unboxedData)
NESTED_ENTRY JIT_BoxFastUP, _TEXT

        mov     rax, [rcx + OFFSETOF__MethodTable__m_pWriteableData]

        ; Check whether the class has not been initialized
        test    dword ptr [rax + OFFSETOF__MethodTableWriteableData__m_dwFlags], MethodTableWriteableData__enum_flag_Unrestored
        jnz     JIT_Box

        mov     r8d, [rcx + OFFSET__MethodTable__m_BaseSize]

        ; m_BaseSize is guaranteed to be a multiple of 8.

        inc     [g_global_alloc_lock]
        jnz     JIT_Box

        mov     rax, [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_ptr]       ; alloc_ptr
        mov     r10, [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_limit]     ; limit_ptr

        add     r8, rax

        cmp     r8, r10
        ja      NoAlloc


        mov     qword ptr [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_ptr], r8     ; update the alloc ptr
        mov     [rax], rcx
        mov     [g_global_alloc_lock], -1

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain_NoScratchArea
endif ; _DEBUG

        ; Check whether the object contains pointers
        test    dword ptr [rcx + OFFSETOF__MethodTable__m_dwFlags], MethodTable__enum_flag_ContainsPointers
        jnz     ContainsPointers

        ; We have no pointers - emit a simple inline copy loop

        mov     ecx, [rcx + OFFSET__MethodTable__m_BaseSize]
        sub     ecx, 18h  ; sizeof(ObjHeader) + sizeof(Object) + last slot

    CopyLoop:
        mov     r8, [rdx+rcx]
        mov     [rax+rcx+8], r8

        sub     ecx, 8
        jge     CopyLoop
        REPRET

    ContainsPointers:

        ; Do call to CopyValueClassUnchecked(object, data, pMT)

        push_vol_reg rax
        alloc_stack 20h
        END_PROLOGUE

        mov     r8, rcx
        lea     rcx, [rax + 8]
        call    CopyValueClassUnchecked

        add     rsp, 20h
        pop     rax
        ret

    NoAlloc:
        mov     [g_global_alloc_lock], -1
        jmp     JIT_Box
NESTED_END JIT_BoxFastUP, _TEXT

LEAF_ENTRY AllocateStringFastUP, _TEXT

        ; We were passed the number of characters in ECX

        ; we need to load the method table for string from the global

        mov     r11, [g_pStringClass]

        ; Instead of doing elaborate overflow checks, we just limit the number of elements
        ; to (LARGE_OBJECT_SIZE - 256)/sizeof(WCHAR) or less.
        ; This will avoid all overflow problems, as well as making sure
        ; big string objects are correctly allocated in the big object heap.

        cmp     ecx, (ASM_LARGE_OBJECT_SIZE - 256)/2
        jae     FramedAllocateString

        mov     r8d, [r11 + OFFSET__MethodTable__m_BaseSize]

        ; Calculate the final size to allocate.
        ; We need to calculate baseSize + cnt*2, then round that up by adding 7 and anding ~7.

        lea     r8d, [r8d + ecx*2 + 7]
        and     r8d, -8

        inc     [g_global_alloc_lock]
        jnz     FramedAllocateString

        mov     rax, [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_ptr]       ; alloc_ptr
        mov     r10, [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_limit]     ; limit_ptr

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     qword ptr [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_ptr], r8     ; update the alloc ptr
        mov     [rax], r11
        mov     [g_global_alloc_lock], -1

        mov     [rax + OFFSETOF__StringObject__m_StringLength], ecx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain_NoScratchArea
endif ; _DEBUG

        ret

    AllocFailed:
        mov     [g_global_alloc_lock], -1
        jmp     FramedAllocateString
LEAF_END AllocateStringFastUP, _TEXT

; HCIMPL2(Object*, JIT_NewArr1, CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size)
LEAF_ENTRY JIT_NewArr1VC_UP, _TEXT

        ; We were passed a type descriptor in RCX, which contains the (shared)
        ; array method table and the element type.

        ; The element count is in RDX

        ; NOTE: if this code is ported for CORINFO_HELP_NEWSFAST_ALIGN8, it will need
        ; to emulate the double-specific behavior of JIT_TrialAlloc::GenAllocArray.

        ; Do a conservative check here.  This is to avoid overflow while doing the calculations.  We don't
        ; have to worry about "large" objects, since the allocation quantum is never big enough for
        ; LARGE_OBJECT_SIZE.

        ; For Value Classes, this needs to be 2^16 - slack (2^32 / max component size), 
        ; The slack includes the size for the array header and round-up ; for alignment.  Use 256 for the
        ; slack value out of laziness.

        ; In both cases we do a final overflow check after adding to the alloc_ptr.

        ; we need to load the true method table from the type desc
        mov     r9, [rcx + OFFSETOF__ArrayTypeDesc__m_TemplateMT - 2]

        FIX_INDIRECTION r9
        
        cmp     rdx, (65535 - 256)
        jae     JIT_NewArr1
  
        movzx   r8d, word ptr [r9 + OFFSETOF__MethodTable__m_dwFlags]  ; component size is low 16 bits
        imul    r8d, edx  ; signed mul, but won't overflow due to length restriction above
        add     r8d, dword ptr [r9 + OFFSET__MethodTable__m_BaseSize]

        ; round the size to a multiple of 8

        add     r8d, 7
        and     r8d, -8

        inc     [g_global_alloc_lock]
        jnz     JIT_NewArr1

        mov     rax, [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_ptr]       ; alloc_ptr
        mov     r10, [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_limit]     ; limit_ptr

        add     r8, rax
        jc      AllocFailed

        cmp     r8, r10
        ja      AllocFailed

        mov     qword ptr [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_ptr], r8     ; update the alloc ptr
        mov     [rax], r9
        mov     [g_global_alloc_lock], -1

        mov     dword ptr [rax + OFFSETOF__ArrayBase__m_NumComponents], edx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain_NoScratchArea
endif ; _DEBUG

        ret

    AllocFailed:
        mov     [g_global_alloc_lock], -1
        jmp     JIT_NewArr1
LEAF_END JIT_NewArr1VC_UP, _TEXT


; HCIMPL2(Object*, JIT_NewArr1, CORINFO_CLASS_HANDLE arrayTypeHnd_, INT_PTR size)
LEAF_ENTRY JIT_NewArr1OBJ_UP, _TEXT

        ; We were passed a type descriptor in RCX, which contains the (shared)
        ; array method table and the element type.

        ; The element count is in RDX

        ; NOTE: if this code is ported for CORINFO_HELP_NEWSFAST_ALIGN8, it will need
        ; to emulate the double-specific behavior of JIT_TrialAlloc::GenAllocArray.

        ; Verifies that LARGE_OBJECT_SIZE fits in 32-bit.  This allows us to do array size
        ; arithmetic using 32-bit registers.
        .erre ASM_LARGE_OBJECT_SIZE lt 100000000h

        cmp     rdx, (ASM_LARGE_OBJECT_SIZE - 256)/8 ; sizeof(void*)
        jae     OversizedArray

        ; we need to load the true method table from the type desc
        mov     r9, [rcx + OFFSETOF__ArrayTypeDesc__m_TemplateMT - 2]

        FIX_INDIRECTION r9

        ; In this case we know the element size is sizeof(void *), or 8 for x64
        ; This helps us in two ways - we can shift instead of multiplying, and
        ; there's no need to align the size either

        mov     r8d, dword ptr [r9 + OFFSET__MethodTable__m_BaseSize]
        lea     r8d, [r8d + edx * 8]

        ; No need for rounding in this case - element size is 8, and m_BaseSize is guaranteed
        ; to be a multiple of 8.

        inc     [g_global_alloc_lock]
        jnz     JIT_NewArr1

        mov     rax, [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_ptr]       ; alloc_ptr
        mov     r10, [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_limit]     ; limit_ptr

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     qword ptr [g_global_alloc_context + OFFSETOF__gc_alloc_context__alloc_ptr], r8     ; update the alloc ptr
        mov     [rax], r9
        mov     [g_global_alloc_lock], -1

        mov     dword ptr [rax + OFFSETOF__ArrayBase__m_NumComponents], edx

ifdef _DEBUG
        call    DEBUG_TrialAllocSetAppDomain_NoScratchArea
endif ; _DEBUG

        ret

    AllocFailed:
        mov     [g_global_alloc_lock], -1

    OversizedArray:
        jmp     JIT_NewArr1
LEAF_END JIT_NewArr1OBJ_UP, _TEXT


NESTED_ENTRY JIT_GetSharedNonGCStaticBase_Slow, _TEXT
        alloc_stack     MIN_SIZE
        END_PROLOGUE

        ; Check if rcx (moduleDomainID) is not a moduleID
        test    rcx, 1
        jz      HaveLocalModule

        CALL_GETAPPDOMAIN

        ; Get the LocalModule
        mov     rax, [rax + OFFSETOF__AppDomain__m_sDomainLocalBlock + OFFSETOF__DomainLocalBlock__m_pModuleSlots]
        ; rcx will always be odd, so: rcx * 4 - 4 <=> (rcx >> 1) * 8
        mov     rcx, [rax + rcx * 4 - 4]

    HaveLocalModule:
        ; If class is not initialized, bail to C++ helper
        test    [rcx + OFFSETOF__DomainLocalModule__m_pDataBlob + rdx], 1
        jz      CallHelper

        mov     rax, rcx
        add     rsp, MIN_SIZE
        ret

    align 16
    CallHelper:
        ; Tail call Jit_GetSharedNonGCStaticBase_Helper
        add     rsp, MIN_SIZE
        jmp     JIT_GetSharedNonGCStaticBase_Helper
NESTED_END JIT_GetSharedNonGCStaticBase_Slow, _TEXT

NESTED_ENTRY JIT_GetSharedNonGCStaticBaseNoCtor_Slow, _TEXT
        alloc_stack     MIN_SIZE
        END_PROLOGUE

        ; Check if rcx (moduleDomainID) is not a moduleID
        test    rcx, 1
        jz      HaveLocalModule

        CALL_GETAPPDOMAIN

        ; Get the LocalModule
        mov     rax, [rax + OFFSETOF__AppDomain__m_sDomainLocalBlock + OFFSETOF__DomainLocalBlock__m_pModuleSlots]
        ; rcx will always be odd, so: rcx * 4 - 4 <=> (rcx >> 1) * 8
        mov     rax, [rax + rcx * 4 - 4]

        add     rsp, MIN_SIZE
        ret

    align 16
    HaveLocalModule:
        mov     rax, rcx
        add     rsp, MIN_SIZE
        ret
NESTED_END JIT_GetSharedNonGCStaticBaseNoCtor_Slow, _TEXT

NESTED_ENTRY JIT_GetSharedGCStaticBase_Slow, _TEXT
        alloc_stack     MIN_SIZE
        END_PROLOGUE

        ; Check if rcx (moduleDomainID) is not a moduleID
        test    rcx, 1
        jz      HaveLocalModule

        CALL_GETAPPDOMAIN

        ; Get the LocalModule
        mov     rax, [rax + OFFSETOF__AppDomain__m_sDomainLocalBlock + OFFSETOF__DomainLocalBlock__m_pModuleSlots]
        ; rcx will always be odd, so: rcx * 4 - 4 <=> (rcx >> 1) * 8
        mov     rcx, [rax + rcx * 4 - 4]

    HaveLocalModule:
        ; If class is not initialized, bail to C++ helper
        test    [rcx + OFFSETOF__DomainLocalModule__m_pDataBlob + rdx], 1
        jz      CallHelper

        mov     rax, [rcx + OFFSETOF__DomainLocalModule__m_pGCStatics]

        add     rsp, MIN_SIZE
        ret

    align 16
    CallHelper:
        ; Tail call Jit_GetSharedGCStaticBase_Helper
        add     rsp, MIN_SIZE
        jmp     JIT_GetSharedGCStaticBase_Helper
NESTED_END JIT_GetSharedGCStaticBase_Slow, _TEXT

NESTED_ENTRY JIT_GetSharedGCStaticBaseNoCtor_Slow, _TEXT
        alloc_stack     MIN_SIZE
        END_PROLOGUE

        ; Check if rcx (moduleDomainID) is not a moduleID
        test    rcx, 1
        jz      HaveLocalModule

        CALL_GETAPPDOMAIN

        ; Get the LocalModule
        mov     rax, [rax + OFFSETOF__AppDomain__m_sDomainLocalBlock + OFFSETOF__DomainLocalBlock__m_pModuleSlots]
        ; rcx will always be odd, so: rcx * 4 - 4 <=> (rcx >> 1) * 8
        mov     rcx, [rax + rcx * 4 - 4]

    HaveLocalModule:
        mov     rax, [rcx + OFFSETOF__DomainLocalModule__m_pGCStatics]

        add     rsp, MIN_SIZE
        ret
NESTED_END JIT_GetSharedGCStaticBaseNoCtor_Slow, _TEXT


MON_ENTER_STACK_SIZE                equ     00000020h
MON_EXIT_STACK_SIZE                 equ     00000068h

ifdef MON_DEBUG
ifdef TRACK_SYNC
MON_ENTER_STACK_SIZE_INLINEGETTHREAD equ     00000020h
MON_EXIT_STACK_SIZE_INLINEGETTHREAD  equ     00000068h
endif
endif

BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX    equ     08000000h   ; syncblk.h
BIT_SBLK_IS_HASHCODE                equ     04000000h   ; syncblk.h
BIT_SBLK_SPIN_LOCK                  equ     10000000h   ; syncblk.h

SBLK_MASK_LOCK_THREADID             equ     000003FFh   ; syncblk.h
SBLK_LOCK_RECLEVEL_INC              equ     00000400h   ; syncblk.h
SBLK_MASK_LOCK_RECLEVEL             equ     0000FC00h   ; syncblk.h

MASK_SYNCBLOCKINDEX                 equ     03FFFFFFh   ; syncblk.h
STATE_CHECK                         equ    0FFFFFFFEh

MT_CTX_PROXY_FLAG                   equ     10000000h

g_pSyncTable    equ ?g_pSyncTable@@3PEAVSyncTableEntry@@EA
g_SystemInfo    equ ?g_SystemInfo@@3U_SYSTEM_INFO@@A
g_SpinConstants equ ?g_SpinConstants@@3USpinConstants@@A

extern g_pSyncTable:QWORD
extern g_SystemInfo:QWORD
extern g_SpinConstants:QWORD

; JITutil_MonEnterWorker(Object* obj, BYTE* pbLockTaken)
extern JITutil_MonEnterWorker:proc
; JITutil_MonTryEnter(Object* obj, INT32 timeout, BYTE* pbLockTaken)
extern JITutil_MonTryEnter:proc
; JITutil_MonExitWorker(Object* obj, BYTE* pbLockTaken)
extern JITutil_MonExitWorker:proc
; JITutil_MonSignal(AwareLock* lock, BYTE* pbLockTaken)
extern JITutil_MonSignal:proc
; JITutil_MonContention(AwareLock* lock, BYTE* pbLockTaken)
extern JITutil_MonContention:proc

ifdef _DEBUG
MON_DEBUG   equ  1
endif

ifdef MON_DEBUG
ifdef TRACK_SYNC
extern EnterSyncHelper:proc
extern LeaveSyncHelper:proc
endif
endif


; This is a frameless helper for entering a monitor on a object.
; The object is in ARGUMENT_REG1.  This tries the normal case (no
; blocking or object allocation) in line and calls a framed helper
; for the other cases.
;
; EXTERN_C void JIT_MonEnterWorker_Slow(Object* obj, /*OUT*/ BYTE* pbLockTaken)
NESTED_ENTRY JIT_MonEnterWorker_Slow, _TEXT
        push_nonvol_reg     rsi

        alloc_stack         MON_ENTER_STACK_SIZE

        save_reg_postrsp    rcx, MON_ENTER_STACK_SIZE + 10h + 0h
        save_reg_postrsp    rdx, MON_ENTER_STACK_SIZE + 10h + 8h
        save_reg_postrsp    r8,  MON_ENTER_STACK_SIZE + 10h + 10h
        save_reg_postrsp    r9,  MON_ENTER_STACK_SIZE + 10h + 18h

        END_PROLOGUE

        ; Check if the instance is NULL
        test    rcx, rcx
        jz      FramedLockHelper

        ; Put pbLockTaken in rsi, this can be null
        mov     rsi, rdx

        ; We store the thread object in r11
        CALL_GETTHREAD
        mov     r11, rax

        ; Initialize delay value for retry with exponential backoff
        mov     r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwInitialDuration]

        ; Check if we can abort here
        mov     eax, dword ptr [r11 + OFFSETOF__Thread__m_State]
        and     eax, THREAD_CATCHATSAFEPOINT_BITS
        ; Go through the slow code path to initiate ThreadAbort
        jnz     FramedLockHelper

        ; r8 will hold the syncblockindex address
        lea     r8, [rcx - OFFSETOF__ObjHeader__SyncBlkIndex]

    RetryThinLock:
        ; Fetch the syncblock dword
        mov     eax, dword ptr [r8]

        ; Check whether we have the "thin lock" layout, the lock is free and the spin lock bit is not set
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX + BIT_SBLK_SPIN_LOCK + SBLK_MASK_LOCK_THREADID + SBLK_MASK_LOCK_RECLEVEL
        jnz     NeedMoreTests

        ; Everything is fine - get the thread id to store in the lock
        mov     edx, dword ptr [r11 + OFFSETOF__Thread__m_ThreadId]

        ; If the thread id is too large, we need a syncblock for sure
        cmp     edx, SBLK_MASK_LOCK_THREADID
        ja      FramedLockHelper

        ; We want to store a new value with the current thread id set in the low 10 bits
        or      edx, eax
   lock cmpxchg dword ptr [r8], edx
        jnz     PrepareToWaitThinLock

        ; Everything went fine and we're done
        add     dword ptr [r11 + OFFSETOF__Thread__m_dwLockCount], 1

        ; Done, leave and set pbLockTaken if we have it
        jmp     LockTaken

    NeedMoreTests:
        ; OK, not the simple case, find out which case it is
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX
        jnz     HaveHashOrSyncBlockIndex

        ; The header is transitioning or the lock, treat this as if the lock was taken
        test    eax, BIT_SBLK_SPIN_LOCK
        jnz     PrepareToWaitThinLock

        ; Here we know we have the "thin lock" layout, but the lock is not free.
        ; It could still be the recursion case, compare the thread id to check
        mov     edx, eax
        and     edx, SBLK_MASK_LOCK_THREADID
        cmp     edx, dword ptr [r11 + OFFSETOF__Thread__m_ThreadId]
        jne     PrepareToWaitThinLock

        ; Ok, the thread id matches, it's the recursion case.
        ; Bump up the recursion level and check for overflow
        lea     edx, [eax + SBLK_LOCK_RECLEVEL_INC]
        test    edx, SBLK_MASK_LOCK_RECLEVEL
        jz      FramedLockHelper

        ; Try to put the new recursion level back. If the header was changed in the meantime
        ; we need a full retry, because the layout could have changed
   lock cmpxchg dword ptr [r8], edx
        jnz     RetryHelperThinLock

        ; Done, leave and set pbLockTaken if we have it
        jmp     LockTaken

    PrepareToWaitThinLock:
        ; If we are on an MP system, we try spinning for a certain number of iterations
        cmp     dword ptr [g_SystemInfo + OFFSETOF__g_SystemInfo__dwNumberOfProcessors], 1
        jle     FramedLockHelper

        ; Exponential backoff; delay by approximately 2*r10 clock cycles
        mov     eax, r10d
    delayLoopThinLock:
        pause   ; indicate to the CPU that we are spin waiting
        sub     eax, 1
        jnz     delayLoopThinLock

        ; Next time, wait a factor longer
        imul    r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwBackoffFactor]

        cmp     r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwMaximumDuration]
        jle     RetryHelperThinLock

        jmp     FramedLockHelper

    RetryHelperThinLock:
        jmp     RetryThinLock

    HaveHashOrSyncBlockIndex:
        ; If we have a hash code already, we need to create a sync block
        test    eax, BIT_SBLK_IS_HASHCODE
        jnz     FramedLockHelper

        ; OK, we have a sync block index, just and out the top bits and grab the synblock index
        and     eax, MASK_SYNCBLOCKINDEX

        ; Get the sync block pointer
        mov     rdx, qword ptr [g_pSyncTable]
        shl     eax, 4h
        mov     rdx, [rdx + rax + OFFSETOF__SyncTableEntry__m_SyncBlock]

        ; Check if the sync block has been allocated
        test    rdx, rdx
        jz      FramedLockHelper

        ; Get a pointer to the lock object
        lea     rdx, [rdx + OFFSETOF__SyncBlock__m_Monitor]

        ; Attempt to acquire the lock
    RetrySyncBlock:
        mov     eax, dword ptr [rdx + OFFSETOF__AwareLock__m_MonitorHeld]
        test    eax, eax
        jne     HaveWaiters

        ; Common case, lock isn't held and there are no waiters. Attempt to
        ; gain ownership ourselves
        xor     ecx, ecx
        inc     ecx
   lock cmpxchg dword ptr [rdx + OFFSETOF__AwareLock__m_MonitorHeld], ecx
        jnz     RetryHelperSyncBlock

        ; Success. Save the thread object in the lock and increment the use count
        mov     qword ptr [rdx + OFFSETOF__AwareLock__m_HoldingThread], r11
        add     dword ptr [rdx + OFFSETOF__AwareLock__m_Recursion], 1
        add     dword ptr [r11 + OFFSETOF__Thread__m_dwLockCount], 1

ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rcx, [rsp + MON_ENTER_STACK_SIZE + 8h]       ; return address
        ; void EnterSyncHelper(UINT_PTR caller, AwareLock* lock)
        call    EnterSyncHelper
endif
endif

        ; Done, leave and set pbLockTaken if we have it
        jmp     LockTaken

        ; It's possible to get here with waiters by no lock held, but in this
        ; case a signal is about to be fired which will wake up the waiter. So
        ; for fairness sake we should wait too.
        ; Check first for recur11ve lock attempts on the same thread.
    HaveWaiters:
        ; Is mutex already owned by current thread?
        cmp     [rdx + OFFSETOF__AwareLock__m_HoldingThread], r11
        jne     PrepareToWait

        ; Yes, bump our use count.
        add     dword ptr [rdx + OFFSETOF__AwareLock__m_Recursion], 1

ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rcx, [rsp + MON_ENTER_STACK_SIZE + 8h]       ; return address
        ; void EnterSyncHelper(UINT_PTR caller, AwareLock* lock)
        call    EnterSyncHelper
endif
endif
        ; Done, leave and set pbLockTaken if we have it
        jmp LockTaken

    PrepareToWait:
        ; If we are on a MP system we try spinning for a certain number of iterations
        cmp     dword ptr [g_SystemInfo + OFFSETOF__g_SystemInfo__dwNumberOfProcessors], 1
        jle     HaveWaiters1

        ; Exponential backoff: delay by approximately 2*r10 clock cycles
        mov     eax, r10d
    delayLoop:
        pause   ; indicate to the CPU that we are spin waiting
        sub     eax, 1
        jnz     delayLoop

        ; Next time, wait a factor longer
        imul    r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwBackoffFactor]

        cmp     r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwMaximumDuration]
        jle     RetrySyncBlock

    HaveWaiters1:
        mov     rcx, rdx
        mov     rdx, rsi
        add     rsp, MON_ENTER_STACK_SIZE
        pop     rsi
        ; void JITutil_MonContention(AwareLock* lock, BYTE* pbLockTaken)
        jmp     JITutil_MonContention

    RetryHelperSyncBlock:
        jmp     RetrySyncBlock

    FramedLockHelper:
        mov     rdx, rsi
        add     rsp, MON_ENTER_STACK_SIZE
        pop     rsi
        ; void JITutil_MonEnterWorker(Object* obj, BYTE* pbLockTaken)
        jmp     JITutil_MonEnterWorker

    align 16
        ; This is sensitive to the potential that pbLockTaken is NULL
    LockTaken:
        test    rsi, rsi
        jz      LockTaken_Exit
        mov     byte ptr [rsi], 1
    LockTaken_Exit:
        add     rsp, MON_ENTER_STACK_SIZE
        pop     rsi
        ret
NESTED_END JIT_MonEnterWorker_Slow, _TEXT

; This is a frameless helper for exiting a monitor on a object.
; The object is in ARGUMENT_REG1.  This tries the normal case (no
; blocking or object allocation) in line and calls a framed helper
; for the other cases.
;
; void JIT_MonExitWorker_Slow(Object* obj, BYTE* pbLockTaken)
NESTED_ENTRY JIT_MonExitWorker_Slow, _TEXT
        alloc_stack         MON_EXIT_STACK_SIZE

        save_reg_postrsp    rcx, MON_EXIT_STACK_SIZE + 8h + 0h
        save_reg_postrsp    rdx, MON_EXIT_STACK_SIZE + 8h + 8h
        save_reg_postrsp    r8,  MON_EXIT_STACK_SIZE + 8h + 10h
        save_reg_postrsp    r9,  MON_EXIT_STACK_SIZE + 8h + 18h

        END_PROLOGUE

        ; pbLockTaken is stored in r10
        mov     r10, rdx

        ; if pbLockTaken is NULL then we got here without a state variable, avoid the
        ; next comparison in that case as it will AV
        test    rdx, rdx
        jz      Null_pbLockTaken

        ; If the lock wasn't taken then we bail quickly without doing anything
        cmp     byte ptr [rdx], 0
        je      LockNotTaken

    Null_pbLockTaken:
        ; Check is the instance is null
        test    rcx, rcx
        jz      FramedLockHelper

        ; The Thread obj address is stored in r11
        CALL_GETTHREAD
        mov     r11, rax

        ; r8 will hold the syncblockindex address
        lea     r8, [rcx - OFFSETOF__ObjHeader__SyncBlkIndex]

    RetryThinLock:
        ; Fetch the syncblock dword
        mov     eax, dword ptr [r8]
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX + BIT_SBLK_SPIN_LOCK
        jnz     NeedMoreTests

        ; Ok, we have a "thin lock" layout - check whether the thread id matches
        mov     edx, eax
        and     edx, SBLK_MASK_LOCK_THREADID
        cmp     edx, dword ptr [r11 + OFFSETOF__Thread__m_ThreadId]
        jne     FramedLockHelper

        ; check the recursion level
        test    eax, SBLK_MASK_LOCK_RECLEVEL
        jne     DecRecursionLevel

        ; It's zero -- we're leaving the lock.
        ; So try to put back a zero thread id.
        ; edx and eax match in the thread id bits, and edx is zero else where, so the xor is sufficient
        xor     edx, eax
   lock cmpxchg dword ptr [r8], edx
        jnz     RetryHelperThinLock

        ; Dec the dwLockCount on the thread
        sub     dword ptr [r11 + OFFSETOF__Thread__m_dwLockCount], 1

        ; Done, leave and set pbLockTaken if we have it
        jmp     LockReleased

    DecRecursionLevel:
        lea     edx, [eax - SBLK_LOCK_RECLEVEL_INC]
   lock cmpxchg dword ptr [r8], edx
        jnz     RetryHelperThinLock

        ; We're done, leave and set pbLockTaken if we have it
        jmp     LockReleased

    NeedMoreTests:
        ; Forward all special cases to the slow helper
        test    eax, BIT_SBLK_IS_HASHCODE + BIT_SBLK_SPIN_LOCK
        jnz     FramedLockHelper

        ; Get the sync block index and use it to compute the sync block pointer
        mov     rdx, qword ptr [g_pSyncTable]
        and     eax, MASK_SYNCBLOCKINDEX
        shl     eax, 4
        mov     rdx, [rdx + rax + OFFSETOF__SyncTableEntry__m_SyncBlock]

        ; Was there a sync block?
        test    rdx, rdx
        jz      FramedLockHelper

        ; Get a pointer to the lock object.
        lea     rdx, [rdx + OFFSETOF__SyncBlock__m_Monitor]

        ; Check if the lock is held.
        cmp     qword ptr [rdx + OFFSETOF__AwareLock__m_HoldingThread], r11
        jne     FramedLockHelper

ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     [rsp + 28h], rcx
        mov     [rsp + 30h], rdx
        mov     [rsp + 38h], r10
        mov     [rsp + 40h], r11

        mov     rcx, [rsp + MON_EXIT_STACK_SIZE ]       ; return address
        ; void LeaveSyncHelper(UINT_PTR caller, AwareLock* lock)
        call    LeaveSyncHelper

        mov     rcx, [rsp + 28h]
        mov     rdx, [rsp + 30h]
        mov     r10, [rsp + 38h]
        mov     r11, [rsp + 40h]
endif
endif

        ; Reduce our recursion count
        sub     dword ptr [rdx + OFFSETOF__AwareLock__m_Recursion], 1
        jz      LastRecursion

        ; Done, leave and set pbLockTaken if we have it
        jmp     LockReleased

    RetryHelperThinLock:
        jmp     RetryThinLock

    FramedLockHelper:
        mov     rdx, r10
        add     rsp, MON_EXIT_STACK_SIZE
        ; void JITutil_MonExitWorker(Object* obj, BYTE* pbLockTaken)
        jmp     JITutil_MonExitWorker

    LastRecursion:
ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rax, [rdx + OFFSETOF__AwareLock__m_HoldingThread]
endif
endif

        sub     dword ptr [r11 + OFFSETOF__Thread__m_dwLockCount], 1
        mov     qword ptr [rdx + OFFSETOF__AwareLock__m_HoldingThread], 0

    Retry:
        mov     eax, dword ptr [rdx + OFFSETOF__AwareLock__m_MonitorHeld]
        lea     r9d, [eax - 1]
   lock cmpxchg dword ptr [rdx + OFFSETOF__AwareLock__m_MonitorHeld], r9d
        jne     RetryHelper

        test    eax, STATE_CHECK
        jne     MustSignal

        ; Done, leave and set pbLockTaken if we have it
        jmp     LockReleased

    MustSignal:
        mov     rcx, rdx
        mov     rdx, r10
        add     rsp, MON_EXIT_STACK_SIZE
        ; void JITutil_MonSignal(AwareLock* lock, BYTE* pbLockTaken)
        jmp     JITutil_MonSignal

    RetryHelper:
        jmp     Retry

    LockNotTaken:
        add     rsp, MON_EXIT_STACK_SIZE
        ret

    align 16
        ; This is sensitive to the potential that pbLockTaken is null
    LockReleased:
        test    r10, r10
        jz      LockReleased_Exit
        mov     byte ptr [r10], 0
    LockReleased_Exit:
        add     rsp, MON_EXIT_STACK_SIZE
        ret
NESTED_END JIT_MonExitWorker_Slow, _TEXT

; This is a frameless helper for trying to enter a monitor on a object.
; The object is in ARGUMENT_REG1 and a timeout in ARGUMENT_REG2. This tries the
; normal case (no object allocation) in line and calls a framed helper for the
; other cases.
;
; void JIT_MonTryEnter_Slow(Object* obj, INT32 timeOut, BYTE* pbLockTaken)
NESTED_ENTRY JIT_MonTryEnter_Slow, _TEXT
        push_nonvol_reg     rsi

        alloc_stack         MON_ENTER_STACK_SIZE

        save_reg_postrsp    rcx, MON_ENTER_STACK_SIZE + 10h + 0h
        save_reg_postrsp    rdx, MON_ENTER_STACK_SIZE + 10h + 8h
        save_reg_postrsp    r8,  MON_ENTER_STACK_SIZE + 10h + 10h
        save_reg_postrsp    r9,  MON_ENTER_STACK_SIZE + 10h + 18h

        END_PROLOGUE
        
        mov     rsi, rdx
        
        ; Check if the instance is NULL
        test    rcx, rcx
        jz      FramedLockHelper

        ; Check if the timeout looks valid
        cmp     rdx, -1
        jl      FramedLockHelper

        ; We store the thread object in r11
        CALL_GETTHREAD
        mov     r11, rax

        ; Initialize delay value for retry with exponential backoff
        mov     r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwInitialDuration]

        ; Check if we can abort here
        mov     eax, dword ptr [r11 + OFFSETOF__Thread__m_State]
        and     eax, THREAD_CATCHATSAFEPOINT_BITS
        ; Go through the slow code path to initiate THreadAbort
        jnz     FramedLockHelper

        ; r9 will hold the syncblockindex address
        lea     r9, [rcx - OFFSETOF__ObjHeader__SyncBlkIndex]

    RetryThinLock:
        ; Fetch the syncblock dword
        mov     eax, dword ptr [r9]

        ; Check whether we have the "thin lock" layout, the lock is free and the spin lock bit is not set
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX + BIT_SBLK_SPIN_LOCK + SBLK_MASK_LOCK_THREADID + SBLK_MASK_LOCK_RECLEVEL
        jne     NeedMoreTests

        ; Everything is fine - get the thread id to store in the lock
        mov     edx, dword ptr [r11 + OFFSETOF__Thread__m_ThreadId]

        ; If the thread id is too large, we need a syncblock for sure
        cmp     edx, SBLK_MASK_LOCK_THREADID
        ja      FramedLockHelper

        ; We want to store a new value with the current thread id set in the low 10 bits
        or      edx, eax
   lock cmpxchg dword ptr [r9], edx
        jnz     RetryHelperThinLock

        ; Got the lock, everything is fine
        add     dword ptr [r11 + OFFSETOF__Thread__m_dwLockCount], 1
        ; Return TRUE
        mov     byte ptr [r8], 1
        add     rsp, MON_ENTER_STACK_SIZE
        pop     rsi
        ret

    NeedMoreTests:
        ; OK, not the simple case, find out which case it is
        test    eax, BIT_SBLK_IS_HASH_OR_SYNCBLKINDEX
        jnz     HaveHashOrSyncBlockIndex

        ; The header is transitioning or the lock
        test    eax, BIT_SBLK_SPIN_LOCK
        jnz     RetryHelperThinLock

        ; Here we know we have the "thin lock" layout, but the lock is not free.
        ; It could still be the recursion case, compare the thread id to check
        mov     edx, eax
        and     edx, SBLK_MASK_LOCK_THREADID
        cmp     edx, dword ptr [r11 + OFFSETOF__Thread__m_ThreadId]
        jne     PrepareToWaitThinLock

        ; Ok, the thread id matches, it's the recursion case.
        ; Dump up the recursion level and check for overflow
        lea     edx, [eax + SBLK_LOCK_RECLEVEL_INC]
        test    edx, SBLK_MASK_LOCK_RECLEVEL
        jz      FramedLockHelper

        ; Try to put the new recursion level back. If the header was changed in the meantime
        ; we need a full retry, because the layout could have changed
   lock cmpxchg dword ptr [r9], edx
        jnz     RetryHelperThinLock

        ; Everything went fine and we're done, return TRUE
        mov     byte ptr [r8], 1
        add     rsp, MON_ENTER_STACK_SIZE
        pop     rsi
        ret

    PrepareToWaitThinLock:
        ; Return failure if timeout is zero
        test    rsi, rsi
        jz     TimeoutZero

        ; If we are on an MP system, we try spinning for a certain number of iterations
        cmp     dword ptr [g_SystemInfo + OFFSETOF__g_SystemInfo__dwNumberOfProcessors], 1
        jle     FramedLockHelper

        ; Exponential backoff; delay by approximately 2*r10d clock cycles
        mov     eax, r10d
    DelayLoopThinLock:
        pause   ; indicate to the CPU that we are spin waiting
        sub     eax, 1
        jnz     DelayLoopThinLock

        ; Next time, wait a factor longer
        imul    r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwBackoffFactor]

        cmp     r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwMaximumDuration]
        jle     RetryHelperThinLock

        jmp     FramedLockHelper

    RetryHelperThinLock:
        jmp     RetryThinLock

    HaveHashOrSyncBlockIndex:
        ; If we have a hash code already, we need to create a sync block
        test    eax, BIT_SBLK_IS_HASHCODE
        jnz     FramedLockHelper

        ; OK, we have a sync block index, just and out the top bits and grab the synblock index
        and     eax, MASK_SYNCBLOCKINDEX

        ; Get the sync block pointer
        mov     rdx, qword ptr [g_pSyncTable]
        shl     eax, 4
        mov     rdx, [rdx + rax + OFFSETOF__SyncTableEntry__m_SyncBlock]

        ; Check if the sync block has been allocated
        test    rdx, rdx
        jz      FramedLockHelper

        ; Get a pointer to the lock object
        lea     rdx, [rdx + OFFSETOF__SyncBlock__m_Monitor]

    RetrySyncBlock:
        ; Attempt to acuire the lock
        mov     eax, dword ptr [rdx + OFFSETOF__AwareLock__m_MonitorHeld]
        test    eax, eax
        jne     HaveWaiters

        ; Common case, lock isn't held and there are no waiters. Attempt to
        ; gain ownership ourselves
        xor     ecx, ecx
        inc     ecx
   lock cmpxchg dword ptr [rdx + OFFSETOF__AwareLock__m_MonitorHeld], ecx
        jnz     RetryHelperSyncBlock

        ; Success. Save the thread object in the lock and increment the use count
        mov     qword ptr [rdx + OFFSETOF__AwareLock__m_HoldingThread], r11
        add     dword ptr [rdx + OFFSETOF__AwareLock__m_Recursion], 1
        add     dword ptr [r11 + OFFSETOF__Thread__m_dwLockCount], 1

ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rcx, [rsp + MON_ENTER_STACK_SIZE + 8h]       ; return address
        ; void EnterSyncHelper(UINT_PTR caller, AwareLock* lock)
        call    EnterSyncHelper
endif
endif

        ; Return TRUE
        mov     byte ptr [r8], 1
        add     rsp, MON_ENTER_STACK_SIZE
        pop     rsi
        ret

        ; It's possible to get here with waiters by no lock held, but in this
        ; case a signal is about to be fired which will wake up the waiter. So
        ; for fairness sake we should wait too.
        ; Check first for recur11ve lock attempts on the same thread.
    HaveWaiters:
        ; Is mutex already owned by current thread?
        cmp     [rdx + OFFSETOF__AwareLock__m_HoldingThread], r11
        jne     PrepareToWait

        ; Yes, bump our use count.
        add     dword ptr [rdx + OFFSETOF__AwareLock__m_Recursion], 1

ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rcx, [rsp + MON_ENTER_STACK_SIZE + 8h]       ; return address
        ; void EnterSyncHelper(UINT_PTR caller, AwareLock* lock)
        call    EnterSyncHelper
endif
endif

        ; Return TRUE
        mov     byte ptr [r8], 1
        add     rsp, MON_ENTER_STACK_SIZE
        pop     rsi
        ret

    PrepareToWait:
        ; Return failure if timeout is zero
        test    rsi, rsi
        jz      TimeoutZero

        ; If we are on an MP system, we try spinning for a certain number of iterations
        cmp     dword ptr [g_SystemInfo + OFFSETOF__g_SystemInfo__dwNumberOfProcessors], 1
        jle     Block
    
        ; Exponential backoff; delay by approximately 2*r10d clock cycles
        mov     eax, r10d
    DelayLoop:
        pause   ; indicate to the CPU that we are spin waiting
        sub     eax, 1
        jnz     DelayLoop
    
        ; Next time, wait a factor longer
        imul    r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwBackoffFactor]
    
        cmp     r10d, dword ptr [g_SpinConstants + OFFSETOF__g_SpinConstants__dwMaximumDuration]
        jle     RetrySyncBlock

        jmp     Block

    TimeoutZero:
        ; Return FALSE
        mov     byte ptr [r8], 0
        add     rsp, MON_ENTER_STACK_SIZE
        pop     rsi
        ret

    RetryHelperSyncBlock:
        jmp     RetrySyncBlock

    Block:
        ; In the Block case we've trashed RCX, restore it
        mov     rcx, [rsp + MON_ENTER_STACK_SIZE + 10h]
    FramedLockHelper:
        mov     rdx, rsi
        add     rsp, MON_ENTER_STACK_SIZE
        pop     rsi
        ; void JITutil_MonTryEnter(Object* obj, UINT32 timeout, BYTE* pbLockTaken)
        jmp     JITutil_MonTryEnter

NESTED_END JIT_MonTryEnter_Slow, _TEXT

MON_ENTER_STATIC_RETURN_SUCCESS macro
        ; pbLockTaken is never null for static helpers
        mov     byte ptr [rdx], 1
        add     rsp, MIN_SIZE
        ret
        
        endm

MON_EXIT_STATIC_RETURN_SUCCESS macro
        ; pbLockTaken is never null for static helpers
        mov     byte ptr [rdx], 0
        add     rsp, MIN_SIZE
        ret
        
        endm


; This is a frameless helper for entering a static monitor on a class.
; The methoddesc is in ARGUMENT_REG1.  This tries the normal case (no
; blocking or object allocation) in line and calls a framed helper
; for the other cases.
;
; void JIT_MonEnterStatic_Slow(AwareLock *lock, BYTE *pbLockTaken)
NESTED_ENTRY JIT_MonEnterStatic_Slow, _TEXT
        alloc_stack         MIN_SIZE
    END_PROLOGUE

        ; Attempt to acquire the lock
    Retry:
        mov     eax, dword ptr [rcx + OFFSETOF__AwareLock__m_MonitorHeld]
        test    eax, eax
        jne     HaveWaiters

        ; Common case; lock isn't held and there are no waiters. Attempt to
        ; gain ownership by ourselves.
        mov     r10d, 1
   lock cmpxchg dword ptr [rcx + OFFSETOF__AwareLock__m_MonitorHeld], r10d
        jnz     RetryHelper

        ; Success. Save the thread object in the lock and increment the use count.
        CALL_GETTHREAD
        
        mov     qword ptr [rcx + OFFSETOF__AwareLock__m_HoldingThread], rax
        add     dword ptr [rcx + OFFSETOF__AwareLock__m_Recursion], 1
        add     dword ptr [rax + OFFSETOF__Thread__m_dwLockCount], 1

ifdef MON_DEBUG
ifdef TRACK_SYNC
        add     rsp, MIN_SIZE
        mov     rdx, rcx
        mov     rcx, [rsp]
        ; void EnterSyncHelper(UINT_PTR caller, AwareLock* lock)
        jmp     EnterSyncHelper
endif
endif
        MON_ENTER_STATIC_RETURN_SUCCESS

        ; It's possible to get here with waiters by with no lock held, in this
        ; case a signal is about to be fired which will wake up a waiter. So
        ; for fairness sake we should wait too.
        ; Check first for recursive lock attempts on the same thread.
    HaveWaiters:
        CALL_GETTHREAD

        ; Is mutex alread owned by current thread?
        cmp     [rcx + OFFSETOF__AwareLock__m_HoldingThread], rax
        jne     PrepareToWait

        ; Yes, bump our use count.
        add     dword ptr [rcx + OFFSETOF__AwareLock__m_Recursion], 1
ifdef MON_DEBUG
ifdef TRACK_SYNC
        mov     rdx, rcx
        mov     rcx, [rsp]
        ; void EnterSyncHelper(UINT_PTR caller, AwareLock* lock)
        add     rsp, MIN_SIZE
        jmp     EnterSyncHelper
endif
endif
        MON_ENTER_STATIC_RETURN_SUCCESS

    PrepareToWait:
        add     rsp, MIN_SIZE
        ; void JITutil_MonContention(AwareLock* obj, BYTE* pbLockTaken)
        jmp     JITutil_MonContention

    RetryHelper:
        jmp     Retry
NESTED_END JIT_MonEnterStatic_Slow, _TEXT

; A frameless helper for exiting a static monitor on a class.
; The methoddesc is in ARGUMENT_REG1.  This tries the normal case (no
; blocking or object allocation) in line and calls a framed helper
; for the other cases.
;
; void JIT_MonExitStatic_Slow(AwareLock *lock, BYTE *pbLockTaken)
NESTED_ENTRY JIT_MonExitStatic_Slow, _TEXT
        alloc_stack     MIN_SIZE
    END_PROLOGUE

ifdef MON_DEBUG
ifdef TRACK_SYNC
        push    rsi
        push    rdi
        mov     rsi, rcx
        mov     rdi, rdx
        mov     rdx, [rsp + 8]
        call    LeaveSyncHelper
        mov     rcx, rsi
        mov     rdx, rdi
        pop     rdi
        pop     rsi
endif
endif

        ; Check if lock is held
        CALL_GETTHREAD

        cmp     [rcx + OFFSETOF__AwareLock__m_HoldingThread], rax
        jne     LockError

        ; Reduce our recursion count
        sub     dword ptr [rcx + OFFSETOF__AwareLock__m_Recursion], 1
        jz      LastRecursion

        MON_EXIT_STATIC_RETURN_SUCCESS

        ; This is the last count we held on this lock, so release the lock
    LastRecursion:
        ; Thead* is in rax
        sub     dword ptr [rax + OFFSETOF__Thread__m_dwLockCount], 1
        mov     qword ptr [rcx + OFFSETOF__AwareLock__m_HoldingThread], 0

    Retry:
        mov     eax, dword ptr [rcx + OFFSETOF__AwareLock__m_MonitorHeld]
        lea     r10d, [eax - 1]
   lock cmpxchg dword ptr [rcx + OFFSETOF__AwareLock__m_MonitorHeld], r10d
        jne     RetryHelper
        test    eax, STATE_CHECK
        jne     MustSignal

        MON_EXIT_STATIC_RETURN_SUCCESS

    MustSignal:
        add     rsp, MIN_SIZE
        ; void JITutil_MonSignal(AwareLock* lock, BYTE* pbLockTaken)
        jmp     JITutil_MonSignal

    RetryHelper:
        jmp     Retry

    LockError:
        mov     rcx, CORINFO_SynchronizationLockException_ASM
        add     rsp, MIN_SIZE
        ; void JIT_InternalThrow(unsigned exceptNum)
        jmp     JIT_InternalThrow
NESTED_END JIT_MonExitStatic_Slow, _TEXT


ifdef _DEBUG

extern Object__DEBUG_SetAppDomain:proc

;
; IN: rax: new object needing the AppDomain ID set..
; OUT: rax, returns original value at entry
;
; all integer register state is preserved
;
DEBUG_TrialAllocSetAppDomain_STACK_SIZE         equ MIN_SIZE + 10h
NESTED_ENTRY DEBUG_TrialAllocSetAppDomain, _TEXT
        push_vol_reg    rax
        push_vol_reg    rcx
        push_vol_reg    rdx
        push_vol_reg    r8
        push_vol_reg    r9
        push_vol_reg    r10
        push_vol_reg    r11
        push_nonvol_reg rbx
        alloc_stack     MIN_SIZE
        END_PROLOGUE

        mov             rbx, rax

        ; get the app domain ptr
        CALL_GETAPPDOMAIN

        ; set the sync block app domain ID
        mov             rcx, rbx
        mov             rdx, rax
        call            Object__DEBUG_SetAppDomain

        ; epilog
        add             rsp, MIN_SIZE
        pop             rbx
        pop             r11
        pop             r10
        pop             r9
        pop             r8
        pop             rdx
        pop             rcx
        pop             rax
        ret
NESTED_END DEBUG_TrialAllocSetAppDomain, _TEXT

NESTED_ENTRY DEBUG_TrialAllocSetAppDomain_NoScratchArea, _TEXT

        push_nonvol_reg rbp
        set_frame       rbp, 0
        END_PROLOGUE

        sub             rsp, 20h
        and             rsp, -16

        call            DEBUG_TrialAllocSetAppDomain

        lea             rsp, [rbp+0]
        pop             rbp
        ret
NESTED_END DEBUG_TrialAllocSetAppDomain_NoScratchArea, _TEXT

endif


        end


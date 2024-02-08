; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: JitHelpers_Slow.asm
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

ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
EXTERN g_card_bundle_table:QWORD
endif

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
        jnb     NoShadow

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
ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        sub     rcx, [g_card_table]
        shr     rcx, 0Ah
        add     rcx, [g_card_bundle_table]
        cmp     byte ptr [rcx], 0FFh
        jne     UpdateCardBundleTable
        REPRET

    UpdateCardBundleTable:
        mov     byte ptr [rcx], 0FFh
endif
        ret

    align 16
    Exit:
        REPRET
LEAF_END_MARKED JIT_WriteBarrier_Debug, _TEXT
endif


extern g_global_alloc_lock:dword
extern g_global_ee_alloc_context:qword

LEAF_ENTRY JIT_TrialAllocSFastSP, _TEXT

        mov     r8d, [rcx + OFFSET__MethodTable__m_BaseSize]

        ; m_BaseSize is guaranteed to be a multiple of 8.

        inc     [g_global_alloc_lock]
        jnz     JIT_NEW

        mov     rax, [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]               ; alloc_ptr
        mov     r10, [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__fast_alloc_helper_limit] ; fast_alloc_helper_limit_ptr

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     qword ptr [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r8      ; update the alloc ptr
        mov     [rax], rcx
        mov     [g_global_alloc_lock], -1

        ret

    AllocFailed:
        mov     [g_global_alloc_lock], -1
        jmp     JIT_NEW
LEAF_END JIT_TrialAllocSFastSP, _TEXT

; HCIMPL2(Object*, JIT_Box, CORINFO_CLASS_HANDLE type, void* unboxedData)
NESTED_ENTRY JIT_BoxFastUP, _TEXT

        ; m_BaseSize is guaranteed to be a multiple of 8.
        mov     r8d, [rcx + OFFSET__MethodTable__m_BaseSize]

        inc     [g_global_alloc_lock]
        jnz     JIT_Box

        mov     rax, [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]               ; alloc_ptr
        mov     r10, [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__fast_alloc_helper_limit] ; fast_alloc_helper_limit_ptr

        add     r8, rax

        cmp     r8, r10
        ja      NoAlloc

        test    rdx, rdx
        je      NullRef

        mov     qword ptr [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r8      ; update the alloc ptr
        mov     [rax], rcx
        mov     [g_global_alloc_lock], -1

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
    NullRef:
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

        ; Calculate the final size to allocate.
        ; We need to calculate baseSize + cnt*2, then round that up by adding 7 and anding ~7.

        lea     r8d, [STRING_BASE_SIZE + ecx*2 + 7]
        and     r8d, -8

        inc     [g_global_alloc_lock]
        jnz     FramedAllocateString

        mov     rax, [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]               ; alloc_ptr
        mov     r10, [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__fast_alloc_helper_limit] ; fast_alloc_helper_limit_ptr

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     qword ptr [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r8      ; update the alloc ptr
        mov     [rax], r11
        mov     [g_global_alloc_lock], -1

        mov     [rax + OFFSETOF__StringObject__m_StringLength], ecx

        ret

    AllocFailed:
        mov     [g_global_alloc_lock], -1
        jmp     FramedAllocateString
LEAF_END AllocateStringFastUP, _TEXT

; HCIMPL2(Object*, JIT_NewArr1VC_UP, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size)
LEAF_ENTRY JIT_NewArr1VC_UP, _TEXT

        ; We were passed a (shared) method table in RCX, which contains the element type.

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

        cmp     rdx, (65535 - 256)
        jae     JIT_NewArr1

        movzx   r8d, word ptr [rcx + OFFSETOF__MethodTable__m_dwFlags]  ; component size is low 16 bits
        imul    r8d, edx  ; signed mul, but won't overflow due to length restriction above
        add     r8d, dword ptr [rcx + OFFSET__MethodTable__m_BaseSize]

        ; round the size to a multiple of 8

        add     r8d, 7
        and     r8d, -8

        inc     [g_global_alloc_lock]
        jnz     JIT_NewArr1

        mov     rax, [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]               ; alloc_ptr
        mov     r10, [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__fast_alloc_helper_limit] ; fast_alloc_helper_limit_ptr

        add     r8, rax
        jc      AllocFailed

        cmp     r8, r10
        ja      AllocFailed

        mov     qword ptr [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r8      ; update the alloc ptr
        mov     [rax], rcx
        mov     [g_global_alloc_lock], -1

        mov     dword ptr [rax + OFFSETOF__ArrayBase__m_NumComponents], edx

        ret

    AllocFailed:
        mov     [g_global_alloc_lock], -1
        jmp     JIT_NewArr1
LEAF_END JIT_NewArr1VC_UP, _TEXT


; HCIMPL2(Object*, JIT_NewArr1OBJ_UP, CORINFO_CLASS_HANDLE arrayMT, INT_PTR size)
LEAF_ENTRY JIT_NewArr1OBJ_UP, _TEXT

        ; We were passed a (shared) method table in RCX, which contains the element type.

        ; The element count is in RDX

        ; NOTE: if this code is ported for CORINFO_HELP_NEWSFAST_ALIGN8, it will need
        ; to emulate the double-specific behavior of JIT_TrialAlloc::GenAllocArray.

        ; Verifies that LARGE_OBJECT_SIZE fits in 32-bit.  This allows us to do array size
        ; arithmetic using 32-bit registers.
        .erre ASM_LARGE_OBJECT_SIZE lt 100000000h

        cmp     rdx, (ASM_LARGE_OBJECT_SIZE - 256)/8 ; sizeof(void*)
        jae     OversizedArray

        ; In this case we know the element size is sizeof(void *), or 8 for x64
        ; This helps us in two ways - we can shift instead of multiplying, and
        ; there's no need to align the size either

        mov     r8d, dword ptr [rcx + OFFSET__MethodTable__m_BaseSize]
        lea     r8d, [r8d + edx * 8]

        ; No need for rounding in this case - element size is 8, and m_BaseSize is guaranteed
        ; to be a multiple of 8.

        inc     [g_global_alloc_lock]
        jnz     JIT_NewArr1

        mov     rax, [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr]               ; alloc_ptr
        mov     r10, [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__fast_alloc_helper_limit] ; fast_alloc_helper_limit_ptr

        add     r8, rax

        cmp     r8, r10
        ja      AllocFailed

        mov     qword ptr [g_global_ee_alloc_context + OFFSETOF__ee_alloc_context__alloc_ptr], r8      ; update the alloc ptr
        mov     [rax], rcx
        mov     [g_global_alloc_lock], -1

        mov     dword ptr [rax + OFFSETOF__ArrayBase__m_NumComponents], edx

        ret

    AllocFailed:
        mov     [g_global_alloc_lock], -1

    OversizedArray:
        jmp     JIT_NewArr1
LEAF_END JIT_NewArr1OBJ_UP, _TEXT


        end


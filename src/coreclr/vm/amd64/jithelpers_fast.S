// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.intel_syntax noprefix
#include "unixasmmacros.inc"
#include "asmconstants.h"

; JIT_ByRefWriteBarrier has weird semantics, see usage in StubLinkerX86.cpp
;
; Entry:
;   RDI - address of ref-field (assigned to)
;   RSI - address of the data  (source)
;   RCX is trashed
;   RAX is trashed when FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP is defined
;   R10 and R11 are trashed
;
;   NOTE: Keep in sync with RBM_CALLEE_TRASH_WRITEBARRIER_BYREF and RBM_CALLEE_GCTRASH_WRITEBARRIER_BYREF
;         if you add more trashed registers.
;
; Exit:
;   RDI, RSI are incremented by SIZEOF(LPVOID)
LEAF_ENTRY JIT_ByRefWriteBarrier, _TEXT
        mov     rcx, [rsi]

; If !WRITE_BARRIER_CHECK do the write first, otherwise we might have to do some ShadowGC stuff
ifndef WRITE_BARRIER_CHECK
        ; rcx is [rsi]
        mov     [rdi], rcx
endif

        ; When WRITE_BARRIER_CHECK is defined _NotInHeap will write the reference
        ; but if it isn't then it will just return.
        ;
        ; See if this is in GCHeap
        cmp     rdi, [C_VAR(g_lowest_address)]
        jb      LOCAL_LABEL(NotInHeap)
        cmp     rdi, [C_VAR(g_highest_address)]
        jnb     LOCAL_LABEL(NotInHeap)

ifdef WRITE_BARRIER_CHECK
        ; we can only trash rcx in this function so in _DEBUG we need to save
        ; some scratch registers.
        push    rax

        ; **ALSO update the shadow GC heap if that is enabled**
        ; Do not perform the work if g_GCShadow is 0
        cmp     C_VAR(g_GCShadow), 0
        je      LOCAL_LABEL(NoShadow)

        ; If we end up outside of the heap don't corrupt random memory
        mov     r10, rdi
        sub     r10, [C_VAR(g_lowest_address)]
        jb      LOCAL_LABEL(NoShadow)

        ; Check that our adjusted destination is somewhere in the shadow gc
        add     r10, [C_VAR(g_GCShadow)]
        cmp     r10, [C_VAR(g_GCShadowEnd]
        jnb     LOCAL_LABEL(NoShadow)

        ; Write ref into real GC
        mov     [rdi], rcx
        ; Write ref into shadow GC
        mov     [r10], rcx

        ; Ensure that the write to the shadow heap occurs before the read from
        ; the GC heap so that race conditions are caught by INVALIDGCVALUE
        mfence

        ; Check that GC/ShadowGC values match
        mov     r11, [rdi]
        mov     rax, [r10]
        cmp     rax, r11
        je      LOCAL_LABEL(DoneShadow)
        mov     r11, INVALIDGCVALUE
        mov     [r10], r11

        jmp     LOCAL_LABEL(DoneShadow)

    ; If we don't have a shadow GC we won't have done the write yet
    LOCAL_LABEL(NoShadow):
        mov     [rdi], rcx

    ; If we had a shadow GC then we already wrote to the real GC at the same time
    ; as the shadow GC so we want to jump over the real write immediately above.
    ; Additionally we know for sure that we are inside the heap and therefore don't
    ; need to replicate the above checks.
    LOCAL_LABEL(DoneShadow):
        pop     rax
endif

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        ; Update the write watch table if necessary
        cmp     byte ptr [C_VAR(g_sw_ww_enabled_for_gc_heap)], 0x0
        je      LOCAL_LABEL(CheckCardTable)
        mov     rax, rdi
        shr     rax, 0xC ; SoftwareWriteWatch::AddressToTableByteIndexShift
        add     rax, qword ptr [C_VAR(g_sw_ww_table)]
        cmp     byte ptr [rax], 0x0
        jne     LOCAL_LABEL(CheckCardTable)
        mov     byte ptr [rax], 0xFF
endif

        ; See if we can just quick out
    LOCAL_LABEL(CheckCardTable):
        cmp     rcx, [C_VAR(g_ephemeral_low)]
        jb      LOCAL_LABEL(Exit)
        cmp     rcx, [C_VAR(g_ephemeral_high)]
        jnb     LOCAL_LABEL(Exit)

        ; do the following checks only if we are allowed to trash rax
        ; otherwise we don't have enough registers
ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        mov     rax, rcx

        mov     cl, [C_VAR(g_region_shr)]
        test    cl, cl
        je      LOCAL_LABEL(SkipCheck)

        ; check if the source is in gen 2 - then it's not an ephemeral pointer
        shr     rax, cl
        add     rax, [C_VAR(g_region_to_generation_table)]
        cmp     byte ptr [rax], 0x82
        je      LOCAL_LABEL(Exit)

        ; check if the destination happens to be in gen 0
        mov     rax, rdi
        shr     rax, cl
        add     rax, [C_VAR(g_region_to_generation_table)]
        cmp     byte ptr [rax], 0
        je      LOCAL_LABEL(Exit)
    LOCAL_LABEL(SkipCheck):

        cmp     [C_VAR(g_region_use_bitwise_write_barrier)], 0
        je      LOCAL_LABEL(CheckCardTableByte)

        ; compute card table bit
        mov     rcx, rdi
        mov     al, 1
        shr     rcx, 8
        and     cl, 7
        shl     al, cl

        ; move current rdi value into rcx and then increment the pointers
        mov     rcx, rdi
        add     rsi, 0x8
        add     rdi, 0x8

        ; Check if we need to update the card table
        ; Calc pCardByte
        shr     rcx, 0xB
        add     rcx, [C_VAR(g_card_table)]

        ; Check if this card table bit is already set
        test    byte ptr [rcx], al
        je      LOCAL_LABEL(SetCardTableBit)
        REPRET

    LOCAL_LABEL(SetCardTableBit):
        lock or byte ptr [rcx], al
        jmp     LOCAL_LABEL(CheckCardBundle)
endif
LOCAL_LABEL(CheckCardTableByte):

        ; move current rdi value into rcx and then increment the pointers
        mov     rcx, rdi
        add     rsi, 0x8
        add     rdi, 0x8

        ; Check if we need to update the card table
        ; Calc pCardByte
        shr     rcx, 0xB
        add     rcx, [C_VAR(g_card_table)]

        ; Check if this card is dirty
        cmp     byte ptr [rcx], 0xFF
        jne     LOCAL_LABEL(UpdateCardTable)
        REPRET

    LOCAL_LABEL(UpdateCardTable):
        mov     byte ptr [rcx], 0xFF

    LOCAL_LABEL(CheckCardBundle):

ifdef FEATURE_MANUALLY_MANAGED_CARD_BUNDLES
        ; check if we need to update the card bundle table
        ; restore destination address from rdi - rdi has been incremented by 8 already
        lea     rcx, [rdi-8]
        shr     rcx, 0x15
        add     rcx, [C_VAR(g_card_bundle_table)]
        cmp     byte ptr [rcx], 0xFF
        jne     LOCAL_LABEL(UpdateCardBundleTable)
        REPRET

    LOCAL_LABEL(UpdateCardBundleTable):
        mov     byte ptr [rcx], 0xFF
endif
        ret

    .balign 16
    LOCAL_LABEL(NotInHeap):
; If WRITE_BARRIER_CHECK then we won't have already done the mov and should do it here
; If !WRITE_BARRIER_CHECK we want _NotInHeap and _Leave to be the same and have both
; 16 byte aligned.
ifdef WRITE_BARRIER_CHECK
        ; rcx is [rsi]
        mov     [rdi], rcx
endif
    LOCAL_LABEL(Exit):
        ; Increment the pointers before leaving
        add     rdi, 0x8
        add     rsi, 0x8
        ret
LEAF_END_MARKED JIT_ByRefWriteBarrier, _TEXT

        // When JIT_WriteBarrier is copied into an allocated page,
        // helpers use this global variable to jump to it. This variable is set in InitThreadManager.
        .global C_FUNC(JIT_WriteBarrier_Loc)
#ifdef TARGET_OSX
        .zerofill __DATA,__common,C_FUNC(JIT_WriteBarrier_Loc),8,3
#else
        .data
    C_FUNC(JIT_WriteBarrier_Loc):
        .quad 0
        .text
#endif

// ------------------------------------------------------------------
// __declspec(naked) void F_CALL_CONV JIT_WriteBarrier_Callable(Object **dst, Object* val)
.balign 16
LEAF_ENTRY  JIT_WriteBarrier_Callable, _TEXT
    // JIT_WriteBarrier(Object** dst, Object* src)
    jmp     [rip + C_FUNC(JIT_WriteBarrier_Loc)]
LEAF_END JIT_WriteBarrier_Callable, _TEXT


// The following helper will access ("probe") a word on each page of the stack
// starting with the page right beneath rsp down to the one pointed to by r11.
// The procedure is needed to make sure that the "guard" page is pushed down below the allocated stack frame.
// The call to the helper will be emitted by JIT in the function/funclet prolog when large (larger than 0x3000 bytes) stack frame is required.
//
// NOTE: On Linux we must advance the stack pointer as we probe - it is not allowed to access 65535 bytes below rsp.
// Since this helper will modify a value of rsp - it must establish the frame pointer.
//
// See also https://github.com/dotnet/runtime/issues/9899#issue-303331518 for more information.

#define PROBE_PAGE_SIZE 0x1000

LEAF_ENTRY JIT_StackProbe, _TEXT
        // On entry:
        //   r11 - points to the lowest address on the stack frame being allocated (i.e. [InitialSp - FrameSize])
        //   rsp - points to some byte on the last probed page
        // On exit:
        //   r11 - is preserved
        //
        // NOTE: this helper will probe at least one page below the one pointed by rsp.

        push_nonvol_reg rbp
        mov     rbp, rsp
        set_cfa_register rbp, 16

    END_PROLOGUE

        and     rsp, -PROBE_PAGE_SIZE  // rsp points to the **lowest address** on the last probed page
                                       // This is done to make the following loop end condition simpler.

LOCAL_LABEL(ProbeLoop):
        sub     rsp, PROBE_PAGE_SIZE   // rsp points to the lowest address of the **next page** to probe
        test    dword ptr [rsp], eax   // rsp points to the lowest address on the **last probed** page
        cmp     rsp, r11
        jg      LOCAL_LABEL(ProbeLoop) // if (rsp > r11), then we need to probe at least one more page.

        RESET_FRAME_WITH_RBP
        ret

LEAF_END_MARKED JIT_StackProbe, _TEXT

LEAF_ENTRY JIT_ValidateIndirectCall, _TEXT
        ret
LEAF_END JIT_ValidateIndirectCall, _TEXT

LEAF_ENTRY JIT_DispatchIndirectCall, _TEXT
        movabs r10, 0xCDCDCDCDCDCDCDCD // The real helper clobbers these registers, so clobber them too in the fake helper
        movabs r11, 0xCDCDCDCDCDCDCDCD
        rex64 jmp rax
LEAF_END JIT_DispatchIndirectCall, _TEXT

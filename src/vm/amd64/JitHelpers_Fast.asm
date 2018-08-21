; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.
; See the LICENSE file in the project root for more information.

; ==++==
;

;
; ==--==
; ***********************************************************************
; File: JitHelpers_Fast.asm, see jithelp.asm for history
;
; Notes: routinues which we believe to be on the hot path for managed 
;        code in most scenarios. 
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

INVALIDGCVALUE          equ     0CCCCCCCDh

ifdef _DEBUG
extern JIT_WriteBarrier_Debug:proc
endif

extern JIT_InternalThrow:proc

extern JITutil_ChkCastInterface:proc
extern JITutil_IsInstanceOfInterface:proc
extern JITutil_ChkCastAny:proc
extern JITutil_IsInstanceOfAny:proc

;EXTERN_C Object* JIT_IsInstanceOfClass(MethodTable* pMT, Object* pObject);
LEAF_ENTRY JIT_IsInstanceOfClass, _TEXT
        ; move rdx into rax in case of a match or null
        mov     rax, rdx

        ; check if the instance is null
        test    rdx, rdx
        je      IsNullInst

        ; check is the MethodTable for the instance matches pMT
        cmp     rcx, qword ptr [rdx]
        jne     JIT_IsInstanceOfClass2

    IsNullInst:
        REPRET
LEAF_END JIT_IsInstanceOfClass, _TEXT

LEAF_ENTRY JIT_IsInstanceOfClass2, _TEXT        
        ; check if the parent class matches.
        ; start by putting the MethodTable for the instance in rdx
        mov     rdx, qword ptr [rdx]
        
    align 16
    CheckParent:
        ; NULL parent MethodTable* indicates that we're at the top of the hierarchy

        ; unroll 0
        mov     rdx, qword ptr [rdx + OFFSETOF__MethodTable__m_pParentMethodTable]
        cmp     rcx, rdx
        je      IsInst
        test    rdx, rdx
        je      DoneWithLoop

        ; unroll 1
        mov     rdx, qword ptr [rdx + OFFSETOF__MethodTable__m_pParentMethodTable]
        cmp     rcx, rdx
        je      IsInst
        test    rdx, rdx
        je      DoneWithLoop

        ; unroll 2
        mov     rdx, qword ptr [rdx + OFFSETOF__MethodTable__m_pParentMethodTable]
        cmp     rcx, rdx
        je      IsInst
        test    rdx, rdx
        je      DoneWithLoop

        ; unroll 3
        mov     rdx, qword ptr [rdx + OFFSETOF__MethodTable__m_pParentMethodTable]
        cmp     rcx, rdx
        je      IsInst
        test    rdx, rdx
        jne     CheckParent

    align 16
    DoneWithLoop:
if METHODTABLE_EQUIVALENCE_FLAGS gt 0
        ; check if the instance is a proxy or has type equivalence
        ; get the MethodTable of the original Object (stored earlier in rax)
        mov     rdx, [rax]
        test    dword ptr [rdx + OFFSETOF__MethodTable__m_dwFlags], METHODTABLE_EQUIVALENCE_FLAGS
        jne     SlowPath
endif ; METHODTABLE_EQUIVALENCE_FLAGS gt 0
        
        ; we didn't find a match in the ParentMethodTable hierarchy
        ; and it isn't a proxy and doesn't have type equivalence, return NULL
        xor     eax, eax
        ret
if METHODTABLE_EQUIVALENCE_FLAGS gt 0
    SlowPath:
        ; Set up the args to call JITutil_IsInstanceOfAny. Note that rcx already contains
        ; the MethodTable*
        mov     rdx, rax    ; rdx = Object*

        ; Call out to JITutil_IsInstanceOfAny to handle the proxy/equivalence case.
        jmp     JITutil_IsInstanceOfAny
endif ; METHODTABLE_EQUIVALENCE_FLAGS gt 0
    ; if it is a null instance then rax is null
    ; if they match then rax contains the instance
    align 16
    IsInst:
        REPRET
LEAF_END JIT_IsInstanceOfClass2, _TEXT

; TODO: this is not necessary... we will be calling JIT_ChkCastClass2 all of the time
;       now that the JIT inlines the null check and the exact MT comparison... Or are
;       they only doing it on the IBC hot path??? Look into that. If it will turn out
;       to be cold then put it down at the bottom.

;EXTERN_C Object* JIT_ChkCastClass(MethodTable* pMT, Object* pObject);
LEAF_ENTRY JIT_ChkCastClass, _TEXT
        ; check if the instance is null
        test    rdx, rdx
        je      IsNullInst

        ; check if the MethodTable for the instance matches pMT
        cmp     rcx, qword ptr [rdx]
        jne     JIT_ChkCastClassSpecial

    IsNullInst:
        ; setup the return value for a match or null
        mov     rax, rdx
        ret
LEAF_END JIT_ChkCastClass, _TEXT

LEAF_ENTRY JIT_ChkCastClassSpecial, _TEXT
        ; save off the instance in case it is a proxy, and to setup
        ; our return value for a match
        mov     rax, rdx

        ; check if the parent class matches.
        ; start by putting the MethodTable for the instance in rdx
        mov     rdx, qword ptr [rdx]
    align 16
    CheckParent:
        ; NULL parent MethodTable* indicates that we're at the top of the hierarchy

        ; unroll 0
        mov     rdx, qword ptr [rdx + OFFSETOF__MethodTable__m_pParentMethodTable]
        cmp     rcx, rdx
        je      IsInst
        test    rdx, rdx
        je      DoneWithLoop

        ; unroll 1
        mov     rdx, qword ptr [rdx + OFFSETOF__MethodTable__m_pParentMethodTable]
        cmp     rcx, rdx
        je      IsInst
        test    rdx, rdx
        je      DoneWithLoop

        ; unroll 2
        mov     rdx, qword ptr [rdx + OFFSETOF__MethodTable__m_pParentMethodTable]
        cmp     rcx, rdx
        je      IsInst
        test    rdx, rdx
        je      DoneWithLoop

        ; unroll 3
        mov     rdx, qword ptr [rdx + OFFSETOF__MethodTable__m_pParentMethodTable]
        cmp     rcx, rdx
        je      IsInst
        test    rdx, rdx
        jne     CheckParent

    align 16
    DoneWithLoop:
        ; Set up the args to call JITutil_ChkCastAny. Note that rcx already contains the MethodTable*
        mov     rdx, rax    ; rdx = Object*

        ; Call out to JITutil_ChkCastAny to handle the proxy case and throw a rich
        ; InvalidCastException in case of failure.
        jmp     JITutil_ChkCastAny

    ; if it is a null instance then rax is null
    ; if they match then rax contains the instance
    align 16
    IsInst:
        REPRET
LEAF_END JIT_ChkCastClassSpecial, _TEXT

FIX_INDIRECTION macro Reg
ifdef FEATURE_PREJIT
        test    Reg, 1
        jz      @F
        mov     Reg, [Reg-1]
    @@:
endif
endm

; PERF TODO: consider prefetching the entire interface map into the cache

; For all bizarre castes this quickly fails and falls back onto the JITutil_IsInstanceOfAny
; helper, this means that all failure cases take the slow path as well.
;
; This can trash r10/r11
LEAF_ENTRY JIT_IsInstanceOfInterface, _TEXT
        test    rdx, rdx
        jz      IsNullInst

        ; get methodtable
        mov     rax, [rdx]
        mov     r11w, word ptr [rax + OFFSETOF__MethodTable__m_wNumInterfaces]

        test    r11w, r11w
        jz      DoBizarre
        
        ; fetch interface map ptr
        mov     rax, [rax + OFFSETOF__MethodTable__m_pInterfaceMap]

        ; r11 holds number of interfaces
        ; rax is pointer to beginning of interface map list
    align 16
    Top:
        ; rax -> InterfaceInfo_t* into the interface map, aligned to 4 entries
        ; use offsets of SIZEOF__InterfaceInfo_t to get at entry 1, 2, 3 in this
        ; block. If we make it through the full 4 without a hit we'll move to
        ; the next block of 4 and try again.

        ; unroll 0
ifdef FEATURE_PREJIT
        mov     r10, [rax + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
        FIX_INDIRECTION r10
        cmp     rcx, r10
else     
        cmp     rcx, [rax + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
endif
        je      Found
        ; move to next entry in list
        dec     r11w
        jz      DoBizarre

        ; unroll 1
ifdef FEATURE_PREJIT
        mov     r10, [rax + SIZEOF__InterfaceInfo_t + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
        FIX_INDIRECTION r10
        cmp     rcx, r10
else     
        cmp     rcx, [rax + SIZEOF__InterfaceInfo_t + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
endif
        je      Found
        ; move to next entry in list
        dec     r11w
        jz      DoBizarre

        ; unroll 2
ifdef FEATURE_PREJIT
        mov     r10, [rax + 2 * SIZEOF__InterfaceInfo_t + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
        FIX_INDIRECTION r10
        cmp     rcx, r10
else     
        cmp     rcx, [rax + 2 * SIZEOF__InterfaceInfo_t + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
endif
        je      Found
        ; move to next entry in list
        dec     r11w
        jz      DoBizarre

        ; unroll 3
ifdef FEATURE_PREJIT
        mov     r10, [rax + 3 * SIZEOF__InterfaceInfo_t + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
        FIX_INDIRECTION r10
        cmp     rcx, r10
else     
        cmp     rcx, [rax + 3 * SIZEOF__InterfaceInfo_t + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
endif
        je      Found
        ; move to next entry in list
        dec     r11w
        jz      DoBizarre

        ; if we didn't find the entry in this loop jump to the next 4 entries in the map
        add     rax, 4 * SIZEOF__InterfaceInfo_t
        jmp     Top

    DoBizarre:
        mov     rax, [rdx]
        test    dword ptr [rax + OFFSETOF__MethodTable__m_dwFlags], METHODTABLE_NONTRIVIALINTERFACECAST_FLAGS
        jnz     NonTrivialCast
        xor     rax,rax
        ret

    align 16
    Found:
    IsNullInst:
        ; return the successful instance
        mov     rax, rdx
        ret

    NonTrivialCast:
        jmp     JITutil_IsInstanceOfInterface
LEAF_END JIT_IsInstanceOfInterface, _TEXT

; For all bizarre castes this quickly fails and falls back onto the JITutil_ChkCastInterface
; helper, this means that all failure cases take the slow path as well.
;
; This can trash r10/r11
LEAF_ENTRY JIT_ChkCastInterface, _TEXT
        test    rdx, rdx
        jz      IsNullInst

        ; get methodtable
        mov     rax, [rdx]
        mov     r11w, word ptr [rax + OFFSETOF__MethodTable__m_wNumInterfaces]

        ; speculatively fetch interface map ptr
        mov     rax, [rax + OFFSETOF__MethodTable__m_pInterfaceMap]

        test    r11w, r11w
        jz      DoBizarre
      
        ; r11 holds number of interfaces
        ; rax is pointer to beginning of interface map list
    align 16
    Top:
        ; rax -> InterfaceInfo_t* into the interface map, aligned to 4 entries
        ; use offsets of SIZEOF__InterfaceInfo_t to get at entry 1, 2, 3 in this
        ; block. If we make it through the full 4 without a hit we'll move to
        ; the next block of 4 and try again.

        ; unroll 0
ifdef FEATURE_PREJIT
        mov     r10, [rax + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
        FIX_INDIRECTION r10
        cmp     rcx, r10
else     
        cmp     rcx, [rax + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
endif
        je      Found
        ; move to next entry in list
        dec     r11w
        jz      DoBizarre

        ; unroll 1
ifdef FEATURE_PREJIT
        mov     r10, [rax + SIZEOF__InterfaceInfo_t + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
        FIX_INDIRECTION r10
        cmp     rcx, r10
else     
        cmp     rcx, [rax + SIZEOF__InterfaceInfo_t + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
endif
        je      Found
        ; move to next entry in list
        dec     r11w
        jz      DoBizarre

        ; unroll 2
ifdef FEATURE_PREJIT
        mov     r10, [rax + 2 * SIZEOF__InterfaceInfo_t + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
        FIX_INDIRECTION r10
        cmp     rcx, r10
else     
        cmp     rcx, [rax + 2 * SIZEOF__InterfaceInfo_t + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
endif
        je      Found
        ; move to next entry in list
        dec     r11w
        jz      DoBizarre

        ; unroll 3
ifdef FEATURE_PREJIT
        mov     r10, [rax + 3 * SIZEOF__InterfaceInfo_t + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
        FIX_INDIRECTION r10
        cmp     rcx, r10
else     
        cmp     rcx, [rax + 3 * SIZEOF__InterfaceInfo_t + OFFSETOF__InterfaceInfo_t__m_pMethodTable]
endif
        je      Found
        ; move to next entry in list
        dec     r11w
        jz      DoBizarre

        ; if we didn't find the entry in this loop jump to the next 4 entries in the map
        add     rax, 4 * SIZEOF__InterfaceInfo_t
        jmp Top

    DoBizarre:
        jmp     JITutil_ChkCastInterface

    align 16
    Found:
    IsNullInst:
        ; return either NULL or the successful instance
        mov     rax, rdx
        ret
LEAF_END JIT_ChkCastInterface, _TEXT

; There is an even more optimized version of these helpers possible which takes
; advantage of knowledge of which way the ephemeral heap is growing to only do 1/2
; that check (this is more significant in the JIT_WriteBarrier case).
;
; Additionally we can look into providing helpers which will take the src/dest from
; specific registers (like x86) which _could_ (??) make for easier register allocation
; for the JIT64, however it might lead to having to have some nasty code that treats
; these guys really special like... :(.
;
; Version that does the move, checks whether or not it's in the GC and whether or not
; it needs to have it's card updated
;
; void JIT_CheckedWriteBarrier(Object** dst, Object* src)
LEAF_ENTRY JIT_CheckedWriteBarrier, _TEXT

        ; When WRITE_BARRIER_CHECK is defined _NotInHeap will write the reference
        ; but if it isn't then it will just return.
        ;
        ; See if this is in GCHeap
        cmp     rcx, [g_lowest_address]
        jb      NotInHeap
        cmp     rcx, [g_highest_address]
        jnb     NotInHeap
        
        jmp     JIT_WriteBarrier

    NotInHeap:
        ; See comment above about possible AV
        mov     [rcx], rdx
        ret
LEAF_END_MARKED JIT_CheckedWriteBarrier, _TEXT

; Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeStart, _TEXT
        ret
LEAF_END JIT_PatchedCodeStart, _TEXT


; This is used by the mechanism to hold either the JIT_WriteBarrier_PreGrow 
; or JIT_WriteBarrier_PostGrow code (depending on the state of the GC). It _WILL_
; change at runtime as the GC changes. Initially it should simply be a copy of the 
; larger of the two functions (JIT_WriteBarrier_PostGrow) to ensure we have created
; enough space to copy that code in.
LEAF_ENTRY JIT_WriteBarrier, _TEXT
        align 16

ifdef _DEBUG
        ; In debug builds, this just contains jump to the debug version of the write barrier by default
        jmp     JIT_WriteBarrier_Debug
endif

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        ; JIT_WriteBarrier_WriteWatch_PostGrow64

        ; Regarding patchable constants:
        ; - 64-bit constants have to be loaded into a register
        ; - The constants have to be aligned to 8 bytes so that they can be patched easily
        ; - The constant loads have been located to minimize NOP padding required to align the constants
        ; - Using different registers for successive constant loads helps pipeline better. Should we decide to use a special
        ;   non-volatile calling convention, this should be changed to use just one register.

        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        ; Update the write watch table if necessary
        mov     rax, rcx
        mov     r8, 0F0F0F0F0F0F0F0F0h
        shr     rax, 0Ch ; SoftwareWriteWatch::AddressToTableByteIndexShift
        NOP_2_BYTE ; padding for alignment of constant
        mov     r9, 0F0F0F0F0F0F0F0F0h
        add     rax, r8
        cmp     byte ptr [rax], 0h
        jne     CheckCardTable
        mov     byte ptr [rax], 0FFh

        NOP_3_BYTE ; padding for alignment of constant

        ; Check the lower and upper ephemeral region bounds
    CheckCardTable:
        cmp     rdx, r9
        jb      Exit

        NOP_3_BYTE ; padding for alignment of constant

        mov     r8, 0F0F0F0F0F0F0F0F0h

        cmp     rdx, r8
        jae     Exit

        nop ; padding for alignment of constant

        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Touch the card table entry, if not already dirty.
        shr     rcx, 0Bh
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rcx + rax], 0FFh
        ret

    align 16
    Exit:
        REPRET
else
        ; JIT_WriteBarrier_PostGrow64

        ; Do the move into the GC .  It is correct to take an AV here, the EH code
        ; figures out that this came from a WriteBarrier and correctly maps it back
        ; to the managed method which called the WriteBarrier (see setup in
        ; InitializeExceptionHandling, vm\exceptionhandling.cpp).
        mov     [rcx], rdx

        NOP_3_BYTE ; padding for alignment of constant

        ; Can't compare a 64 bit immediate, so we have to move them into a
        ; register.  Values of these immediates will be patched at runtime.
        ; By using two registers we can pipeline better.  Should we decide to use
        ; a special non-volatile calling convention, this should be changed to
        ; just one.

        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Check the lower and upper ephemeral region bounds
        cmp     rdx, rax
        jb      Exit

        nop ; padding for alignment of constant

        mov     r8, 0F0F0F0F0F0F0F0F0h

        cmp     rdx, r8
        jae     Exit

        nop ; padding for alignment of constant

        mov     rax, 0F0F0F0F0F0F0F0F0h

        ; Touch the card table entry, if not already dirty.
        shr     rcx, 0Bh
        cmp     byte ptr [rcx + rax], 0FFh
        jne     UpdateCardTable
        REPRET

    UpdateCardTable:
        mov     byte ptr [rcx + rax], 0FFh
        ret

    align 16
    Exit:
        REPRET
endif

    ; make sure this guy is bigger than any of the other guys
    align 16
        nop
LEAF_END_MARKED JIT_WriteBarrier, _TEXT

; Mark start of the code region that we patch at runtime
LEAF_ENTRY JIT_PatchedCodeLast, _TEXT
        ret
LEAF_END JIT_PatchedCodeLast, _TEXT

; JIT_ByRefWriteBarrier has weird symantics, see usage in StubLinkerX86.cpp
;
; Entry:
;   RDI - address of ref-field (assigned to)
;   RSI - address of the data  (source)
;   RCX is trashed
;   RAX is trashed when FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP is defined
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
        cmp     rdi, [g_lowest_address]
        jb      NotInHeap
        cmp     rdi, [g_highest_address]
        jnb     NotInHeap

ifdef WRITE_BARRIER_CHECK
        ; we can only trash rcx in this function so in _DEBUG we need to save
        ; some scratch registers.
        push    r10
        push    r11
        push    rax

        ; **ALSO update the shadow GC heap if that is enabled**
        ; Do not perform the work if g_GCShadow is 0
        cmp     g_GCShadow, 0
        je      NoShadow

        ; If we end up outside of the heap don't corrupt random memory
        mov     r10, rdi
        sub     r10, [g_lowest_address]
        jb      NoShadow

        ; Check that our adjusted destination is somewhere in the shadow gc
        add     r10, [g_GCShadow]
        cmp     r10, [g_GCShadowEnd]
        ja      NoShadow

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
        je      DoneShadow
        mov     r11, INVALIDGCVALUE
        mov     [r10], r11

        jmp     DoneShadow

    ; If we don't have a shadow GC we won't have done the write yet
    NoShadow:
        mov     [rdi], rcx

    ; If we had a shadow GC then we already wrote to the real GC at the same time
    ; as the shadow GC so we want to jump over the real write immediately above.
    ; Additionally we know for sure that we are inside the heap and therefore don't
    ; need to replicate the above checks.
    DoneShadow:
        pop     rax
        pop     r11
        pop     r10
endif

ifdef FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP
        ; Update the write watch table if necessary
        cmp     byte ptr [g_sw_ww_enabled_for_gc_heap], 0h
        je      CheckCardTable
        mov     rax, rdi
        shr     rax, 0Ch ; SoftwareWriteWatch::AddressToTableByteIndexShift
        add     rax, qword ptr [g_sw_ww_table]
        cmp     byte ptr [rax], 0h
        jne     CheckCardTable
        mov     byte ptr [rax], 0FFh
endif

        ; See if we can just quick out
    CheckCardTable:
        cmp     rcx, [g_ephemeral_low]
        jb      Exit
        cmp     rcx, [g_ephemeral_high]
        jnb     Exit

        ; move current rdi value into rcx and then increment the pointers
        mov     rcx, rdi
        add     rsi, 8h
        add     rdi, 8h

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
    NotInHeap:
; If WRITE_BARRIER_CHECK then we won't have already done the mov and should do it here
; If !WRITE_BARRIER_CHECK we want _NotInHeap and _Leave to be the same and have both
; 16 byte aligned.
ifdef WRITE_BARRIER_CHECK
        ; rcx is [rsi]
        mov     [rdi], rcx
endif
    Exit:
        ; Increment the pointers before leaving
        add     rdi, 8h
        add     rsi, 8h
        ret
LEAF_END_MARKED JIT_ByRefWriteBarrier, _TEXT


g_pObjectClass      equ     ?g_pObjectClass@@3PEAVMethodTable@@EA

EXTERN  g_pObjectClass:qword
extern ArrayStoreCheck:proc
extern ObjIsInstanceOfNoGC:proc

; TODO: put definition for this in asmconstants.h
CanCast equ     1

;__declspec(naked) void F_CALL_CONV JIT_Stelem_Ref(PtrArray* array, unsigned idx, Object* val)
LEAF_ENTRY JIT_Stelem_Ref, _TEXT
        ; check for null PtrArray*
        test    rcx, rcx
        je      ThrowNullReferenceException

        ; we only want the lower 32-bits of edx, it might be dirty
        or      edx, edx

        ; check that index is in bounds
        cmp     edx, dword ptr [rcx + OFFSETOF__PtrArray__m_NumComponents] ; 8h -> array size offset
        jae     ThrowIndexOutOfRangeException

        ; r10 = Array MT
        mov     r10, [rcx]

        ; if we're assigning a null object* then we don't need a write barrier
        test    r8, r8
        jz      AssigningNull

        mov     r9, [r10 + OFFSETOF__MethodTable__m_ElementType]   ; 10h -> typehandle offset

        ; check for exact match
        cmp     r9, [r8]
        jne     NotExactMatch

    DoWrite:
        lea     rcx, [rcx + 8*rdx + OFFSETOF__PtrArray__m_Array]
        mov     rdx, r8

        ; JIT_WriteBarrier(Object** dst, Object* src)
        jmp     JIT_WriteBarrier

    AssigningNull:
        ; write barrier is not needed for assignment of NULL references
        mov     [rcx + 8*rdx + OFFSETOF__PtrArray__m_Array], r8
        ret
            
    NotExactMatch:
        cmp     r9, [g_pObjectClass]
        je      DoWrite

        jmp     JIT_Stelem_Ref__ObjIsInstanceOfNoGC_Helper
                                
    ThrowNullReferenceException:
        mov     rcx, CORINFO_NullReferenceException_ASM
        jmp     JIT_InternalThrow
        
    ThrowIndexOutOfRangeException:
        mov     rcx, CORINFO_IndexOutOfRangeException_ASM
        jmp     JIT_InternalThrow        
LEAF_END JIT_Stelem_Ref, _TEXT

NESTED_ENTRY JIT_Stelem_Ref__ObjIsInstanceOfNoGC_Helper, _TEXT
        alloc_stack         MIN_SIZE
        save_reg_postrsp    rcx, MIN_SIZE + 8h
        save_reg_postrsp    rdx, MIN_SIZE + 10h
        save_reg_postrsp    r8,  MIN_SIZE + 18h
    END_PROLOGUE

        ; need to get TypeHandle before setting rcx to be the Obj* because that trashes the PtrArray*
        mov     rdx, r9
        mov     rcx, r8

        ; TypeHandle::CastResult ObjIsInstanceOfNoGC(Object *pElement, TypeHandle toTypeHnd)
        call    ObjIsInstanceOfNoGC

        mov     rcx, [rsp + MIN_SIZE + 8h]
        mov     rdx, [rsp + MIN_SIZE + 10h]
        mov     r8,  [rsp + MIN_SIZE + 18h]

        cmp     eax, CanCast
        jne     NeedCheck

        lea     rcx, [rcx + 8*rdx + OFFSETOF__PtrArray__m_Array]
        mov     rdx, r8
        add     rsp, MIN_SIZE

        ; JIT_WriteBarrier(Object** dst, Object* src)
        jmp     JIT_WriteBarrier

    NeedCheck:
        add     rsp, MIN_SIZE
        jmp     JIT_Stelem_Ref__ArrayStoreCheck_Helper
NESTED_END JIT_Stelem_Ref__ObjIsInstanceOfNoGC_Helper, _TEXT

; Need to save r8 to provide a stack address for the Object*
NESTED_ENTRY JIT_Stelem_Ref__ArrayStoreCheck_Helper, _TEXT
        alloc_stack     MIN_SIZE
        save_reg_postrsp    rcx, MIN_SIZE + 8h
        save_reg_postrsp    rdx, MIN_SIZE + 10h
        save_reg_postrsp    r8,  MIN_SIZE + 18h
    END_PROLOGUE

        lea     rcx, [rsp + MIN_SIZE + 18h]
        lea     rdx, [rsp + MIN_SIZE + 8h]

        ; HCIMPL2(FC_INNER_RET, ArrayStoreCheck, Object** pElement, PtrArray** pArray)
        call    ArrayStoreCheck

        mov     rcx, [rsp + MIN_SIZE + 8h]
        mov     rdx, [rsp + MIN_SIZE + 10h]
        mov     r8,  [rsp + MIN_SIZE + 18h]

        lea     rcx, [rcx + 8*rdx + OFFSETOF__PtrArray__m_Array]
        mov     rdx, r8
        add     rsp, MIN_SIZE

        ; JIT_WriteBarrier(Object** dst, Object* src)
        jmp     JIT_WriteBarrier

NESTED_END JIT_Stelem_Ref__ArrayStoreCheck_Helper, _TEXT


extern JIT_FailFast:proc
extern s_gsCookie:qword

OFFSETOF_GSCOOKIE                   equ 0h
OFFSETOF_FRAME                      equ OFFSETOF_GSCOOKIE + \
                                        8h

; 
; incoming:
;
;       rsp ->  return address
;                 :
;
; Stack Layout:
; 
; rsp-> callee scratch
; + 8h  callee scratch
; +10h  callee scratch
; +18h  callee scratch
;       :
;       stack arguments
;       :
; r13-> gsCookie
; + 8h      __VFN_table
; +10h      m_Next
; +18h      m_pGCLayout
; +20h      m_padding
; +28h      m_rdi
; +30h      m_rsi
; +38h      m_rbx
; +40h      m_rbp
; +48h      m_r12
; +50h      m_r13
; +58h      m_r14
; +60h      m_r15
; +68h      m_ReturnAddress
; r12 ->  // Caller's SP
;
; r14 = GetThread();
; r15 = GetThread()->GetFrame(); // For restoring/popping the frame
; 
NESTED_ENTRY TailCallHelperStub, _TEXT
        PUSH_CALLEE_SAVED_REGISTERS

        alloc_stack             48h     ; m_padding, m_pGCLayout, m_Next, __VFN_table, gsCookie, outgoing shadow area

        set_frame               r13, 20h
    END_PROLOGUE

        ;
        ; This part is never executed, but we keep it here for reference
        ;
        int 3

if 0 ne 0
        ; Save the caller's SP
        mov     r12, rsp + ...

        ;
        ; fully initialize the TailCallFrame
        ;
        call    TCF_GETMETHODFRAMEVPTR
        mov     [r13 + OFFSETOF_FRAME], rax

        mov     rax, s_gsCookie
        mov     [r13 + OFFSETOF_GSCOOKIE], rax

        ;
        ; link the TailCallFrame
        ;
        INLINE_GETTHREAD r14
        mov     r15, [r14 + OFFSETOF__Thread__m_pFrame]        
        mov     [r13 + OFFSETOF_FRAME + OFFSETOF__Frame__m_Next], r15
        lea     r10, [r13 + OFFSETOF_FRAME]
        mov     [r14 + OFFSETOF__Thread__m_pFrame], r10
endif

        ; the pretend call would be here
        ; with the return address pointing this this real epilog

PATCH_LABEL JIT_TailCallHelperStub_ReturnAddress

        ; our epilog (which also unlinks the TailCallFrame)

ifdef _DEBUG
        mov     rcx, s_gsCookie
        cmp     [r13 + OFFSETOF_GSCookie], rcx
        je      GoodGSCookie
        call    JIT_FailFast
GoodGSCookie:
endif ; _DEBUG

        ;
        ; unlink the TailCallFrame
        ;
        mov     [r14 + OFFSETOF__Thread__m_pFrame], r15

        ; 
        ; epilog
        ;

        lea     rsp, [r13 + 28h]
        POP_CALLEE_SAVED_REGISTERS
        ret

NESTED_END TailCallHelperStub, _TEXT

        end


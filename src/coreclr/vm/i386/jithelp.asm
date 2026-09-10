; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: JIThelp.asm
;
; ***********************************************************************
;
;  *** NOTE:  If you make changes to this file, propagate the changes to
;             jithelp.s in this directory

; This contains JITinterface routines that are 100% x86 assembly

        .586
        .model  flat

        include asmconstants.inc
        include asmmacros.inc

        option  casemap:none
        .code
ARGUMENT_REG1           equ     ecx
ARGUMENT_REG2           equ     edx
JIT_LLsh                        TEXTEQU <_JIT_LLsh@0>
JIT_LRsh                        TEXTEQU <_JIT_LRsh@0>
JIT_LRsz                        TEXTEQU <_JIT_LRsz@0>
JIT_LMul                        TEXTEQU <@JIT_LMul@16>
JIT_TailCall                    TEXTEQU <_JIT_TailCall@0>
JIT_TailCallLeave               TEXTEQU <_JIT_TailCallLeave@0>
JIT_TailCallVSDLeave            TEXTEQU <_JIT_TailCallVSDLeave@0>
JIT_TailCallHelper              TEXTEQU <_JIT_TailCallHelper@4>
JIT_TailCallReturnFromVSD       TEXTEQU <_JIT_TailCallReturnFromVSD@0>

g_pPollGC                       TEXTEQU <_g_pPollGC>
g_TrapReturningThreads          TEXTEQU <_g_TrapReturningThreads>

ifdef FEATURE_HIJACK
EXTERN  JIT_TailCallHelper:PROC
endif
EXTERN @JIT_FailFast@0:PROC

EXTERN g_pPollGC:DWORD
EXTERN g_TrapReturningThreads:DWORD


.686P
.XMM
; The following macro is needed because of a MASM issue with the
; movsd mnemonic
;
$movsd MACRO op1, op2
    LOCAL begin_movsd, end_movsd
begin_movsd:
    movupd op1, op2
end_movsd:
    org begin_movsd
    db 0F2h
    org end_movsd
ENDM
.586

; The following macro is used to match the JITs
; multi-byte NOP sequence
$nop3 MACRO
    db 090h
    db 090h
    db 090h
ENDM



;*********************************************************************/
;llshl - long shift left
;
;Purpose:
;   Does a Long Shift Left (signed and unsigned are identical)
;   Shifts a long left any number of bits.
;
;       NOTE:  This routine has been adapted from the Microsoft CRTs.
;
;Entry:
;   EDX:EAX - long value to be shifted
;       ECX - number of bits to shift by
;
;Exit:
;   EDX:EAX - shifted value
;
        ALIGN 16
PUBLIC JIT_LLsh
JIT_LLsh PROC
; Reduce shift amount mod 64
        and     ecx, 63
; Handle shifts of between bits 0 and 31
        cmp     ecx, 32
        jae     short LLshMORE32
        shld    edx,eax,cl
        shl     eax,cl
        ret
; Handle shifts of between bits 32 and 63
LLshMORE32:
        ; The x86 shift instructions only use the lower 5 bits.
        mov     edx,eax
        xor     eax,eax
        shl     edx,cl
        ret
JIT_LLsh ENDP


;*********************************************************************/
;LRsh - long shift right
;
;Purpose:
;   Does a signed Long Shift Right
;   Shifts a long right any number of bits.
;
;       NOTE:  This routine has been adapted from the Microsoft CRTs.
;
;Entry:
;   EDX:EAX - long value to be shifted
;       ECX - number of bits to shift by
;
;Exit:
;   EDX:EAX - shifted value
;
        ALIGN 16
PUBLIC JIT_LRsh
JIT_LRsh PROC
; Reduce shift amount mod 64
        and     ecx, 63
; Handle shifts of between bits 0 and 31
        cmp     ecx, 32
        jae     short LRshMORE32
        shrd    eax,edx,cl
        sar     edx,cl
        ret
; Handle shifts of between bits 32 and 63
LRshMORE32:
        ; The x86 shift instructions only use the lower 5 bits.
        mov     eax,edx
        sar     edx, 31
        sar     eax,cl
        ret
JIT_LRsh ENDP


;*********************************************************************/
; LRsz:
;Purpose:
;   Does a unsigned Long Shift Right
;   Shifts a long right any number of bits.
;
;       NOTE:  This routine has been adapted from the Microsoft CRTs.
;
;Entry:
;   EDX:EAX - long value to be shifted
;       ECX - number of bits to shift by
;
;Exit:
;   EDX:EAX - shifted value
;
        ALIGN 16
PUBLIC JIT_LRsz
JIT_LRsz PROC
; Reduce shift amount mod 64
        and     ecx, 63
; Handle shifts of between bits 0 and 31
        cmp     ecx, 32
        jae     short LRszMORE32
        shrd    eax,edx,cl
        shr     edx,cl
        ret
; Handle shifts of between bits 32 and 63
LRszMORE32:
        ; The x86 shift instructions only use the lower 5 bits.
        mov     eax,edx
        xor     edx,edx
        shr     eax,cl
        ret
JIT_LRsz ENDP

;*********************************************************************/
; LMul:
;Purpose:
;   Does a long multiply (same for signed/unsigned)
;
;       NOTE:  This routine has been adapted from the Microsoft CRTs.
;
;Entry:
;   Parameters are passed on the stack:
;               1st pushed: multiplier (QWORD)
;               2nd pushed: multiplicand (QWORD)
;
;Exit:
;   EDX:EAX - product of multiplier and multiplicand
;
        ALIGN 16
PUBLIC JIT_LMul
JIT_LMul PROC

;       AHI, BHI : upper 32 bits of A and B
;       ALO, BLO : lower 32 bits of A and B
;
;             ALO * BLO
;       ALO * BHI
; +     BLO * AHI
; ---------------------

        mov     eax,[esp + 8]   ; AHI
        mov     ecx,[esp + 16]  ; BHI
        or      ecx,eax         ;test for both hiwords zero.
        mov     ecx,[esp + 12]  ; BLO
        jnz     LMul_hard       ;both are zero, just mult ALO and BLO

        mov     eax,[esp + 4]
        mul     ecx

        ret     16              ; callee restores the stack

LMul_hard:
        push    ebx

        mul     ecx             ;eax has AHI, ecx has BLO, so AHI * BLO
        mov     ebx,eax         ;save result

        mov     eax,[esp + 8]   ; ALO
        mul     dword ptr [esp + 20] ;ALO * BHI
        add     ebx,eax         ;ebx = ((ALO * BHI) + (AHI * BLO))

        mov     eax,[esp + 8]   ; ALO   ;ecx = BLO
        mul     ecx             ;so edx:eax = ALO*BLO
        add     edx,ebx         ;now edx has all the LO*HI stuff

        pop     ebx

        ret     16              ; callee restores the stack

JIT_LMul ENDP

;*********************************************************************/
;

        ; a fake virtual stub dispatch register indirect callsite
        $nop3
        call    dword ptr [eax]


PUBLIC JIT_TailCallReturnFromVSD
JIT_TailCallReturnFromVSD:
ifdef _DEBUG
        nop                         ; blessed callsite
endif
        call    VSDHelperLabel      ; keep call-ret count balanced.
VSDHelperLabel:

; Stack at this point :
;    ...
; m_ReturnAddress
; m_regs
; m_CallerAddress
; m_pThread
; frame identifier
; &VSDHelperLabel
OffsetOfTailCallFrame = 4 ; Offset to start of TailCallFrame, includes only the &VSDHelperLabel

; ebx = pThread

        ; remove the padding frame from the chain
        mov     esi, dword ptr [esp+OffsetOfTailCallFrame+4]    ; esi = TailCallFrame::m_Next
        mov     dword ptr [ebx + Thread_m_pFrame], esi

        ; skip the frame
        add     esp, 16     ; &VSDHelperLabel, vtbl, m_Next, m_CallerAddress

        pop     edi         ; restore callee saved registers
        pop     esi
        pop     ebx
        pop     ebp

        ret                 ; return to m_ReturnAddress

;------------------------------------------------------------------------------
;

PUBLIC JIT_TailCall
JIT_TailCall PROC

; the stack layout at this point is:
;
;   ebp+8+4*nOldStackArgs   <- end of argument destination
;    ...                       ...
;   ebp+8+                     old args (size is nOldStackArgs)
;    ...                       ...
;   ebp+8                   <- start of argument destination
;   ebp+4                   ret addr
;   ebp+0                   saved ebp
;   ebp-c                   saved ebx, esi, edi (if have callee saved regs = 1)
;
;                           other stuff (local vars) in the jitted callers' frame
;
;   esp+20+4*nNewStackArgs  <- end of argument source
;    ...                       ...
;   esp+20+                    new args (size is nNewStackArgs) to be passed to the target of the tail-call
;    ...                       ...
;   esp+20                  <- start of argument source
;   esp+16                  nOldStackArgs
;   esp+12                  nNewStackArgs
;   esp+8                   flags (1 = have callee saved regs, 2 = virtual stub dispatch)
;   esp+4                   target addr
;   esp+0                   retaddr
;
;   If you change this function, make sure you update code:TailCallStubManager as well.

RetAddr         equ 0
TargetAddr      equ 4
nNewStackArgs   equ 12
nOldStackArgs   equ 16
NewArgs         equ 20

; extra space is incremented as we push things on the stack along the way
ExtraSpace      = 0

        push    0           ; Thread*

        ; save ArgumentRegisters
        push    ecx
        push    edx

        ; eax = GetThread(). Trashes edx
        INLINE_GETTHREAD eax, edx

        mov     [esp + 8], eax

ExtraSpace      = 12    ; pThread, ecx, edx

ifdef FEATURE_HIJACK
        ; Make sure that the EE does have the return address patched. So we can move it around.
        test    dword ptr [eax+Thread_m_State], TS_Hijacked_ASM
        jz      NoHijack

        ; JIT_TailCallHelper(Thread *)
        push    eax
        call    JIT_TailCallHelper  ; this is __stdcall

NoHijack:
endif

        mov     edx, dword ptr [esp+ExtraSpace+JIT_TailCall_StackOffsetToFlags]           ; edx = flags

        mov     eax, dword ptr [esp+ExtraSpace+nOldStackArgs]   ; eax = nOldStackArgs
        mov     ecx, dword ptr [esp+ExtraSpace+nNewStackArgs]   ; ecx = nNewStackArgs

        ; restore callee saved registers
        ; <TODO>@TODO : esp based - doesnt work with localloc</TODO>
        test    edx, 1
        jz      NoCalleeSaveRegisters

        mov     edi, dword ptr [ebp-4]              ; restore edi
        mov     esi, dword ptr [ebp-8]              ; restore esi
        mov     ebx, dword ptr [ebp-12]             ; restore ebx

NoCalleeSaveRegisters:

        push    dword ptr [ebp+4]                   ; save the original return address for later
        push    edi
        push    esi

ExtraSpace      = 24    ; pThread, ecx, edx, orig retaddr, edi, esi
CallersEsi      = 0
CallersEdi      = 4
OrigRetAddr     = 8
pThread         = 20

        lea     edi, [ebp+8+4*eax]                  ; edi = the end of argument destination
        lea     esi, [esp+ExtraSpace+NewArgs+4*ecx] ; esi = the end of argument source

        mov     ebp, dword ptr [ebp]        ; restore ebp (do not use ebp as scratch register to get a good stack trace in debugger)

        test    edx, 2
        jnz     VSDTailCall

        ; copy the arguments to the final destination
        test    ecx, ecx
        jz      ArgumentsCopied
ArgumentCopyLoop:
        ; At this point, this is the value of the registers :
        ; edi = end of argument dest
        ; esi = end of argument source
        ; ecx = nNewStackArgs
        mov     eax, dword ptr [esi-4]
        sub     edi, 4
        sub     esi, 4
        mov     dword ptr [edi], eax
        dec     ecx
        jnz     ArgumentCopyLoop
ArgumentsCopied:

        ; edi = the start of argument destination

        mov     eax, dword ptr [esp+4+4]                    ; return address
        mov     ecx, dword ptr [esp+ExtraSpace+TargetAddr]  ; target address

        mov     dword ptr [edi-4], eax      ; return address
        mov     dword ptr [edi-8], ecx      ; target address

        lea     eax, [edi-8]                ; new value for esp

        pop     esi
        pop     edi
        pop     ecx         ; skip original return address
        pop     edx
        pop     ecx

        mov     esp, eax

PUBLIC JIT_TailCallLeave    ; add a label here so that TailCallStubManager can access it
JIT_TailCallLeave:
        retn                ; Will branch to targetAddr.  This matches the
                            ; "call" done by JITted code, keeping the
                            ; call-ret count balanced.

        ;----------------------------------------------------------------------
VSDTailCall:
        ;----------------------------------------------------------------------

        ; For the Virtual Stub Dispatch, we create a fake callsite to fool
        ; the callsite probes. In order to create the call site, we need to insert TailCallFrame
        ; if we do not have one already.
        ;
        ; ecx = nNewStackArgs
        ; esi = the end of argument source
        ; edi = the end of argument destination
        ;
        ; The stub has pushed the following onto the stack at this point :
        ; pThread, ecx, edx, orig retaddr, edi, esi


        cmp     dword ptr [esp+OrigRetAddr], JIT_TailCallReturnFromVSD
        jz      VSDTailCallFrameInserted_DoSlideUpArgs ; There is an exiting TailCallFrame that can be reused

        ; try to allocate space for the frame / check whether there is enough space
        ; If there is sufficient space, we will setup the frame and then slide
        ; the arguments up the stack. Else, we first need to slide the arguments
        ; down the stack to make space for the TailCallFrame
        sub     edi, (SIZEOF_TailCallFrame)
        cmp     edi, esi
        jae     VSDSpaceForFrameChecked

        ; There is not sufficient space to wedge in the TailCallFrame without
        ; overwriting the new arguments.
        ; We need to allocate the extra space on the stack,
        ; and slide down the new arguments

        mov     eax, esi
        sub     eax, edi
        sub     esp, eax

        mov     eax, ecx                        ; to subtract the size of arguments
        mov     edx, ecx                        ; for counter

        neg     eax

        ; copy down the arguments to the final destination, need to copy all temporary storage as well
        add     edx, (ExtraSpace+NewArgs)/4

        lea     esi, [esi+4*eax-(ExtraSpace+NewArgs)]
        lea     edi, [edi+4*eax-(ExtraSpace+NewArgs)]

VSDAllocFrameCopyLoop:
        mov     eax, dword ptr [esi]
        mov     dword ptr [edi], eax
        add     esi, 4
        add     edi, 4
        dec     edx
        jnz     VSDAllocFrameCopyLoop

        ; the argument source and destination are same now
        mov     esi, edi

VSDSpaceForFrameChecked:

        ; At this point, we have enough space on the stack for the TailCallFrame,
        ; and we may already have slided down the arguments

        mov     edx, dword ptr [esp+OrigRetAddr]        ; orig return address
        mov     dword ptr [edi], FRAMETYPE_TailCallFrame  ; FrameIdentifier::TailCallFrame
        mov     dword ptr [edi+28], edx         ; TailCallFrame::m_ReturnAddress

        mov     eax, dword ptr [esp+CallersEdi]         ; restored edi
        mov     edx, dword ptr [esp+CallersEsi]         ; restored esi
        mov     dword ptr [edi+12], eax         ; TailCallFrame::m_regs::edi
        mov     dword ptr [edi+16], edx         ; TailCallFrame::m_regs::esi
        mov     dword ptr [edi+20], ebx         ; TailCallFrame::m_regs::ebx
        mov     dword ptr [edi+24], ebp         ; TailCallFrame::m_regs::ebp

        mov     ebx, dword ptr [esp+pThread]            ; ebx = pThread

        mov     eax, dword ptr [ebx+Thread_m_pFrame]
        mov     dword ptr [edi+4], eax          ; TailCallFrame::m_pNext
        mov     dword ptr [ebx+Thread_m_pFrame], edi    ; hook the new frame into the chain

        ; setup ebp chain
        lea     ebp, [edi+24]                   ; TailCallFrame::m_regs::ebp

        ; Do not copy arguments again if they are in place already
        ; Otherwise, we will need to slide the new arguments up the stack
        cmp     esi, edi
        jne     VSDTailCallFrameInserted_DoSlideUpArgs

        ; At this point, we must have already previously slided down the new arguments,
        ; or the TailCallFrame is a perfect fit
        ; set the caller address
        mov     edx, dword ptr [esp+ExtraSpace+RetAddr] ; caller address
        mov     dword ptr [edi+8], edx         ; TailCallFrame::m_CallerAddress

        ; adjust edi as it would by copying
        neg     ecx
        lea     edi, [edi+4*ecx]

        jmp     VSDArgumentsCopied

VSDTailCallFrameInserted_DoSlideUpArgs:
        ; set the caller address
        mov     edx, dword ptr [esp+ExtraSpace+RetAddr] ; caller address
        mov     dword ptr [edi+8], edx          ; TailCallFrame::m_CallerAddress

        ; copy the arguments to the final destination
        test    ecx, ecx
        jz      VSDArgumentsCopied
VSDArgumentCopyLoop:
        mov     eax, dword ptr [esi-4]
        sub     edi, 4
        sub     esi, 4
        mov     dword ptr [edi], eax
        dec     ecx
        jnz     VSDArgumentCopyLoop
VSDArgumentsCopied:

        ; edi = the start of argument destination

        mov     ecx, dword ptr [esp+ExtraSpace+TargetAddr]   ; target address

        mov     dword ptr [edi-4], JIT_TailCallReturnFromVSD ; return address
        mov     dword ptr [edi-12], ecx     ; address of indirection cell
        mov     ecx, [ecx]
        mov     dword ptr [edi-8], ecx      ; target address

        ; skip original return address and saved esi, edi
        add     esp, 12

        pop     edx
        pop     ecx

        lea     esp, [edi-12]   ; new value for esp
        pop     eax

PUBLIC JIT_TailCallVSDLeave ; add a label here so that TailCallStubManager can access it
JIT_TailCallVSDLeave:
        retn                ; Will branch to targetAddr.  This matches the
                            ; "call" done by JITted code, keeping the
                            ; call-ret count balanced.

JIT_TailCall ENDP

;------------------------------------------------------------------------------

; The following helper will access ("probe") a word on each page of the stack
; starting with the page right beneath esp down to the one pointed to by eax.
; The procedure is needed to make sure that the "guard" page is pushed down below the allocated stack frame.
; The call to the helper will be emitted by JIT in the function prolog when large (larger than 0x3000 bytes) stack frame is required.
;
; NOTE: this helper will modify a value of esp and must establish the frame pointer.
PROBE_PAGE_SIZE equ 1000h

_JIT_StackProbe@0 PROC public
    ; On entry:
    ;   eax - the lowest address of the stack frame being allocated (i.e. [InitialSp - FrameSize])
    ;
    ; NOTE: this helper will probe at least one page below the one pointed by esp.
    push    ebp
    mov     ebp, esp

    and     esp, -PROBE_PAGE_SIZE ; esp points to the **lowest address** on the last probed page
                                  ; This is done to make the loop end condition simpler.
ProbeLoop:
    test    [esp - 4], eax        ; esp points to the lowest address on the **last probed** page
    sub     esp, PROBE_PAGE_SIZE  ; esp points to the lowest address of the **next page** to probe
    cmp     esp, eax
    jg      ProbeLoop             ; if esp > eax, then we need to probe at least one more page.

    mov     esp, ebp
    pop     ebp
    ret

_JIT_StackProbe@0 ENDP

PUBLIC _JIT_StackProbe_End@0
_JIT_StackProbe_End@0 PROC
    ret
_JIT_StackProbe_End@0 ENDP

@JIT_PollGC@0 PROC public
    cmp [g_TrapReturningThreads], 0
    jnz JIT_PollGCRarePath
    ret
JIT_PollGCRarePath:
    mov eax, g_pPollGC
    jmp eax
@JIT_PollGC@0 ENDP

    end

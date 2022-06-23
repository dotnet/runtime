;; Licensed to the .NET Foundation under one or more agreements.
;; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

PROBE_SAVE_FLAGS_EVERYTHING     equ DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_ALL_SCRATCH
PROBE_SAVE_FLAGS_RAX_IS_GCREF   equ DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_RAX + PTFF_RAX_IS_GCREF

;;
;; See PUSH_COOP_PINVOKE_FRAME, this macro is very similar, but also saves RAX and accepts the register
;; bitmask in RCX
;;
;; On entry:
;;  - BITMASK: bitmask describing pushes, may be volatile register or constant value
;;  - RAX: managed function return value, may be an object or byref
;;  - preserved regs: need to stay preserved, may contain objects or byrefs
;;  - extraStack bytes of stack have already been allocated
;;
;; INVARIANTS
;; - The macro assumes it is called from a prolog, prior to a frame pointer being setup.
;; - All preserved registers remain unchanged from their values in managed code.
;;
PUSH_PROBE_FRAME macro threadReg, trashReg, extraStack, BITMASK

    push_vol_reg    rax                         ; save RAX, it might contain an objectref
    lea             trashReg, [rsp + 10h + extraStack]
    push_vol_reg    trashReg                    ; save caller's RSP
    push_nonvol_reg r15                         ; save preserved registers
    push_nonvol_reg r14                         ;   ..
    push_nonvol_reg r13                         ;   ..
    push_nonvol_reg r12                         ;   ..
    push_nonvol_reg rdi                         ;   ..
    push_nonvol_reg rsi                         ;   ..
    push_nonvol_reg rbx                         ;   ..
    push_vol_reg    BITMASK                     ; save the register bitmask passed in by caller
    push_vol_reg    threadReg                   ; Thread * (unused by stackwalker)
    push_nonvol_reg rbp                         ; save caller's RBP
    mov             trashReg, [rsp + 12*8 + extraStack]  ; Find the return address
    push_vol_reg    trashReg                    ; save m_RIP
    lea             trashReg, [rsp + 0]         ; trashReg == address of frame

    ;; allocate scratch space and any required alignment
    alloc_stack     20h + 10h + (extraStack AND (10h-1))

    ;; save xmm0 in case it's being used as a return value
    movdqa          [rsp + 20h], xmm0

    ; link the frame into the Thread
    mov             [threadReg + OFFSETOF__Thread__m_pDeferredTransitionFrame], trashReg
endm

;;
;; Remove the frame from a previous call to PUSH_PROBE_FRAME from the top of the stack and restore preserved
;; registers and return value to their values from before the probe was called (while also updating any
;; object refs or byrefs).
;;
;; NOTE: does NOT deallocate the 'extraStack' portion of the stack, the user of this macro must do that.
;;
POP_PROBE_FRAME macro extraStack
    movdqa      xmm0, [rsp + 20h]
    add         rsp, 20h + 10h + (extraStack AND (10h-1)) + 8
    pop         rbp
    pop         rax     ; discard Thread*
    pop         rax     ; discard BITMASK
    pop         rbx
    pop         rsi
    pop         rdi
    pop         r12
    pop         r13
    pop         r14
    pop         r15
    pop         rax     ; discard caller RSP
    pop         rax
endm

;;
;; Macro to clear the hijack state. This is safe to do because the suspension code will not Unhijack this
;; thread if it finds it at an IP that isn't managed code.
;;
;; Register state on entry:
;;  RDX: thread pointer
;;
;; Register state on exit:
;;  RCX: trashed
;;
ClearHijackState macro
        xor         ecx, ecx
        mov         [rdx + OFFSETOF__Thread__m_ppvHijackedReturnAddressLocation], rcx
        mov         [rdx + OFFSETOF__Thread__m_pvHijackedReturnAddress], rcx
endm


;;
;; The prolog for all GC suspension hijacks (normal and stress). Fixes up the hijacked return address, and
;; clears the hijack state.
;;
;; Register state on entry:
;;  All registers correct for return to the original return address.
;;
;; Register state on exit:
;;  RCX: trashed
;;  RDX: thread pointer
;;
FixupHijackedCallstack macro

        ;; rdx <- GetThread(), TRASHES rcx
        INLINE_GETTHREAD rdx, rcx

        ;;
        ;; Fix the stack by pushing the original return address
        ;;
        mov         rcx, [rdx + OFFSETOF__Thread__m_pvHijackedReturnAddress]
        push        rcx

        ClearHijackState
endm

;;
;; Set the Thread state and wait for a GC to complete.
;;
;; Register state on entry:
;;  RBX: thread pointer
;;
;; Register state on exit:
;;  RBX: thread pointer
;;  All other registers trashed
;;

EXTERN RhpWaitForGCNoAbort : PROC

WaitForGCCompletion macro
        test        dword ptr [rbx + OFFSETOF__Thread__m_ThreadStateFlags], TSF_SuppressGcStress + TSF_DoNotTriggerGc
        jnz         @F

        mov         rcx, [rbx + OFFSETOF__Thread__m_pDeferredTransitionFrame]
        call        RhpWaitForGCNoAbort
@@:

endm


EXTERN RhpPInvokeExceptionGuard : PROC

;;
;;
;;
;; GC Probe Hijack targets
;;
;;
NESTED_ENTRY RhpGcProbeHijackScalar, _TEXT, RhpPInvokeExceptionGuard
        END_PROLOGUE
        FixupHijackedCallstack
        mov         ecx, DEFAULT_FRAME_SAVE_FLAGS
        jmp         RhpGcProbe
NESTED_END RhpGcProbeHijackScalar, _TEXT

NESTED_ENTRY RhpGcProbeHijackObject, _TEXT, RhpPInvokeExceptionGuard
        END_PROLOGUE
        FixupHijackedCallstack
        mov         ecx, DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_RAX + PTFF_RAX_IS_GCREF
        jmp         RhpGcProbe
NESTED_END RhpGcProbeHijackObject, _TEXT

NESTED_ENTRY RhpGcProbeHijackByref, _TEXT, RhpPInvokeExceptionGuard
        END_PROLOGUE
        FixupHijackedCallstack
        mov         ecx, DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_RAX + PTFF_RAX_IS_BYREF
        jmp         RhpGcProbe
NESTED_END RhpGcProbeHijackByref, _TEXT

ifdef FEATURE_GC_STRESS
;;
;;
;; GC Stress Hijack targets
;;
;;
LEAF_ENTRY RhpGcStressHijackScalar, _TEXT
        FixupHijackedCallstack
        mov         ecx, DEFAULT_FRAME_SAVE_FLAGS
        jmp         RhpGcStressProbe
LEAF_END RhpGcStressHijackScalar, _TEXT

LEAF_ENTRY RhpGcStressHijackObject, _TEXT
        FixupHijackedCallstack
        mov         ecx, DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_RAX + PTFF_RAX_IS_GCREF
        jmp         RhpGcStressProbe
LEAF_END RhpGcStressHijackObject, _TEXT

LEAF_ENTRY RhpGcStressHijackByref, _TEXT
        FixupHijackedCallstack
        mov         ecx, DEFAULT_FRAME_SAVE_FLAGS + PTFF_SAVE_RAX + PTFF_RAX_IS_BYREF
        jmp         RhpGcStressProbe
LEAF_END RhpGcStressHijackByref, _TEXT

;;
;; Worker for our GC stress probes.  Do not call directly!!
;; Instead, go through RhpGcStressHijack{Scalar|Object|Byref}.
;; This worker performs the GC Stress work and returns to the original return address.
;;
;; Register state on entry:
;;  RDX: thread pointer
;;  RCX: register bitmask
;;
;; Register state on exit:
;;  Scratch registers, except for RAX, have been trashed
;;  All other registers restored as they were when the hijack was first reached.
;;
NESTED_ENTRY RhpGcStressProbe, _TEXT
        PUSH_PROBE_FRAME rdx, rax, 0, rcx
        END_PROLOGUE

        call        REDHAWKGCINTERFACE__STRESSGC

        POP_PROBE_FRAME 0
        ret
NESTED_END RhpGcStressProbe, _TEXT

endif ;; FEATURE_GC_STRESS

EXTERN RhpThrowHwEx : PROC

NESTED_ENTRY RhpGcProbe, _TEXT
        test        [RhpTrapThreads], TrapThreadsFlags_TrapThreads
        jnz         @f
        ret
@@:
        PUSH_PROBE_FRAME rdx, rax, 0, rcx
        END_PROLOGUE

        mov         rbx, rdx
        WaitForGCCompletion

        mov         rax, [rbx + OFFSETOF__Thread__m_pDeferredTransitionFrame]
        test        dword ptr [rax + OFFSETOF__PInvokeTransitionFrame__m_Flags], PTFF_THREAD_ABORT
        jnz         Abort
        POP_PROBE_FRAME 0
        ret
Abort:
        POP_PROBE_FRAME 0
        mov         rcx, STATUS_REDHAWK_THREAD_ABORT
        pop         rdx         ;; return address as exception RIP
        jmp         RhpThrowHwEx ;; Throw the ThreadAbortException as a special kind of hardware exception

NESTED_END RhpGcProbe, _TEXT


ifdef FEATURE_GC_STRESS
;; PAL_LIMITED_CONTEXT, 6 xmm regs to save, 2 scratch regs to save, plus 20h bytes for scratch space
RhpHijackForGcStress_FrameSize equ SIZEOF__PAL_LIMITED_CONTEXT + 6*10h + 2*8h + 20h

; -----------------------------------------------------------------------------------------------------------
; RhpHijackForGcStress
;
; Called at the beginning of the epilog when a method is bound with /gcstress
;
; N.B. -- Leaf frames may not have aligned the stack or reserved any scratch space on the stack.  Also, in
;         order to have a resonable stacktrace in the debugger, we must use the .pushframe unwind directive.
;
; N.B. #2 -- The "EH jump epilog" codegen depends on rcx/rdx being preserved across this call.  We currently
;            will trash R8-R11, but we can do better, if necessary.
;
NESTED_ENTRY RhpHijackForGcStress, _TEXT

        lea         r10, [rsp+8]        ;; save the original RSP (prior to call)
        mov         r11, [rsp]          ;; get the return address

        ;; Align the stack
        and         rsp, -16

        ;; Push the expected "machine frame" for the unwinder to see.  All that it looks at is the RSP and
        ;; RIP, so we push zero for the others.
        xor     r8, r8
        push    r8              ;; just aligning the stack
        push    r8              ;; SS
        push    r10             ;; original RSP
        push    r8              ;; EFLAGS
        push    r8              ;; CS
        push    r11             ;; return address

        ; Tell the unwinder that the frame is there now
        .pushframe

        alloc_stack     RhpHijackForGcStress_FrameSize
        END_PROLOGUE

        ;; Save xmm scratch regs -- this is probably overkill, only the return value reg is
        ;; likely to be interesting at this point, but it's a bit ambiguous.
        movdqa      [rsp + 20h + 0*10h], xmm0
        movdqa      [rsp + 20h + 1*10h], xmm1
        movdqa      [rsp + 20h + 2*10h], xmm2
        movdqa      [rsp + 20h + 3*10h], xmm3
        movdqa      [rsp + 20h + 4*10h], xmm4
        movdqa      [rsp + 20h + 5*10h], xmm5

        mov         [rsp + 20h + 6*10h + 0*8h], rcx
        mov         [rsp + 20h + 6*10h + 1*8h], rdx

        ;;
        ;; Setup a PAL_LIMITED_CONTEXT that looks like what you'd get if you had suspended this thread at the
        ;; IP after the call to this helper.
        ;;
        ;; This is very likely overkill since the calculation of the return address should only need RSP and
        ;; RBP, but this is test code, so I'm not too worried about efficiency.
        ;;
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__IP],  r11     ; rip at callsite
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rsp], r10     ; rsp at callsite
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rbp], rbp
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rdi], rdi
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rsi], rsi
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rax], rax
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rbx], rbx

        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__R12], r12
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__R13], r13
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__R14], r14
        mov         [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__R15], r15

        lea         rcx, [rsp + 20h + 6*10h + 2*8h]   ;; address of PAL_LIMITED_CONTEXT
        call        THREAD__HIJACKFORGCSTRESS

        ;; Note: we only restore the scratch registers here. No GC has occurred, so restoring
        ;; the callee saved ones is unnecessary.
        mov         rax, [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rax]
        mov         rcx, [rsp + 20h + 6*10h + 0*8h]
        mov         rdx, [rsp + 20h + 6*10h + 1*8h]

        ;; Restore xmm scratch regs
        movdqa      xmm0, [rsp + 20h + 0*10h]
        movdqa      xmm1, [rsp + 20h + 1*10h]
        movdqa      xmm2, [rsp + 20h + 2*10h]
        movdqa      xmm3, [rsp + 20h + 3*10h]
        movdqa      xmm4, [rsp + 20h + 4*10h]
        movdqa      xmm5, [rsp + 20h + 5*10h]

        ;; epilog
        mov         r10, [rsp + 20h + 6*10h + 2*8h + OFFSETOF__PAL_LIMITED_CONTEXT__Rsp]
        lea         rsp, [r10 - 8]              ;; adjust RSP to point back at the return address
        ret
NESTED_END RhpHijackForGcStress, _TEXT

endif ;; FEATURE_GC_STRESS


;;
;; The following functions are _jumped_ to when we need to transfer control from one method to another for EH
;; dispatch. These are needed to properly coordinate with the GC hijacking logic. We are essentially replacing
;; the return from the throwing method with a jump to the handler in the caller, but we need to be aware of
;; any return address hijack that may be in place for GC suspension. These routines use a quick test of the
;; return address against a specific GC hijack routine, and then fixup the stack pointer to what it would be
;; after a real return from the throwing method. Then, if we are not hijacked we can simply jump to the
;; handler in the caller.
;;
;; If we are hijacked, then we jump to a routine that will unhijack appropriatley and wait for the GC to
;; complete. There are also variants for GC stress.
;;
;; Note that at this point we are eiher hijacked or we are not, and this will not change until we return to
;; managed code. It is an invariant of the system that a thread will only attempt to hijack or unhijack
;; another thread while the target thread is suspended in managed code, and this is _not_ managed code.
;;
;; Register state on entry:
;;  RAX: pointer to this function (i.e., trash)
;;  RCX: reference to the exception object.
;;  RDX: handler address we want to jump to.
;;  RBX, RSI, RDI, RBP, and R12-R15 are all already correct for return to the caller.
;;  The stack still contains the return address.
;;
;; Register state on exit:
;;  RSP: what it would be after a complete return to the caler.
;;  RDX: TRASHED
;;
RTU_EH_JUMP_HELPER macro funcName, hijackFuncName, isStress, stressFuncName
LEAF_ENTRY funcName, _TEXT
        lea         rax, [hijackFuncName]
        cmp         [rsp], rax
        je          RhpGCProbeForEHJump

IF isStress EQ 1
        lea         rax, [stressFuncName]
        cmp         [rsp], rax
        je          RhpGCStressProbeForEHJump
ENDIF

        ;; We are not hijacked, so we can return to the handler.
        ;; We return to keep the call/return prediction balanced.
        mov         [rsp], rdx  ; Update the return address
        ret

LEAF_END funcName, _TEXT
endm

;; We need an instance of the helper for each possible hijack function. The binder has enough
;; information to determine which one we need to use for any function.
RTU_EH_JUMP_HELPER RhpEHJumpScalar,         RhpGcProbeHijackScalar, 0, 0
RTU_EH_JUMP_HELPER RhpEHJumpObject,         RhpGcProbeHijackObject, 0, 0
RTU_EH_JUMP_HELPER RhpEHJumpByref,          RhpGcProbeHijackByref,  0, 0
ifdef FEATURE_GC_STRESS
RTU_EH_JUMP_HELPER RhpEHJumpScalarGCStress, RhpGcProbeHijackScalar, 1, RhpGcStressHijackScalar
RTU_EH_JUMP_HELPER RhpEHJumpObjectGCStress, RhpGcProbeHijackObject, 1, RhpGcStressHijackObject
RTU_EH_JUMP_HELPER RhpEHJumpByrefGCStress,  RhpGcProbeHijackByref,  1, RhpGcStressHijackByref
endif

;;
;; Macro to setup our frame and adjust the location of the EH object reference for EH jump probe funcs.
;;
;; Register state on entry:
;;  RAX: scratch
;;  RCX: reference to the exception object.
;;  RDX: handler address we want to jump to.
;;  RBX, RSI, RDI, RBP, and R12-R15 are all already correct for return to the caller.
;;  The stack is as if we are just about to returned from the call
;;
;; Register state on exit:
;;  RAX: reference to the exception object
;;  RCX: scratch
;;  RDX: thread pointer
;;
EHJumpProbeProlog_extraStack = 1*8
EHJumpProbeProlog macro
        push_nonvol_reg rdx         ; save the handler address so we can jump to it later
        mov             rax, rcx    ; move the ex object reference into rax so we can report it

        ;; rdx <- GetThread(), TRASHES rcx
        INLINE_GETTHREAD rdx, rcx

        ;; Fix the stack by patching the original return address
        mov         rcx, [rdx + OFFSETOF__Thread__m_pvHijackedReturnAddress]
        mov         [rsp + EHJumpProbeProlog_extraStack], rcx

        ClearHijackState

        ; TRASHES r10
        PUSH_PROBE_FRAME rdx, r10, EHJumpProbeProlog_extraStack, PROBE_SAVE_FLAGS_RAX_IS_GCREF

        END_PROLOGUE
endm

;;
;; Macro to re-adjust the location of the EH object reference, cleanup the frame, and make the
;; final jump to the handler for EH jump probe funcs.
;;
;; Register state on entry:
;;  RAX: reference to the exception object
;;  RCX: scratch
;;  RDX: scratch
;;
;; Register state on exit:
;;  RSP: correct for return to the caller
;;  RCX: reference to the exception object
;;  RDX: trashed
;;
EHJumpProbeEpilog macro
        POP_PROBE_FRAME EHJumpProbeProlog_extraStack
        mov         rcx, rax    ; Put the EX obj ref back into rcx for the handler.

        pop         rax         ; Recover the handler address.
        mov         [rsp], rax  ; Update the return address
        ret
endm

;;
;; We are hijacked for a normal GC (not GC stress), so we need to unhijcak and wait for the GC to complete.
;;
;; Register state on entry:
;;  RAX: scratch
;;  RCX: reference to the exception object.
;;  RDX: handler address we want to jump to.
;;  RBX, RSI, RDI, RBP, and R12-R15 are all already correct for return to the caller.
;;  The stack is as if we have tail called to this function (rsp points to return address).
;;
;; Register state on exit:
;;  RSP: correct for return to the caller
;;  RBP: previous ebp frame
;;  RCX: reference to the exception object
;;
NESTED_ENTRY RhpGCProbeForEHJump, _TEXT
        EHJumpProbeProlog

ifdef _DEBUG
        ;;
        ;; If we get here, then we have been hijacked for a real GC, and our SyncState must
        ;; reflect that we've been requested to synchronize.

        test        [RhpTrapThreads], TrapThreadsFlags_TrapThreads
        jnz         @F

        call        RhDebugBreak
@@:
endif ;; _DEBUG

        mov         rbx, rdx
        WaitForGCCompletion

        EHJumpProbeEpilog

NESTED_END RhpGCProbeForEHJump, _TEXT

ifdef FEATURE_GC_STRESS
;;
;; We are hijacked for GC Stress (not a normal GC) so we need to invoke the GC stress helper.
;;
;; Register state on entry:
;;  RAX: scratch
;;  RCX: reference to the exception object.
;;  RDX: handler address we want to jump to.
;;  RBX, RSI, RDI, RBP, and R12-R15 are all already correct for return to the caller.
;;  The stack is as if we have tail called to this function (rsp points to return address).
;;
;; Register state on exit:
;;  RSP: correct for return to the caller
;;  RBP: previous ebp frame
;;  RCX: reference to the exception object
;;
NESTED_ENTRY RhpGCStressProbeForEHJump, _TEXT
        EHJumpProbeProlog

        call        REDHAWKGCINTERFACE__STRESSGC

        EHJumpProbeEpilog

NESTED_END RhpGCStressProbeForEHJump, _TEXT

g_pTheRuntimeInstance equ ?g_pTheRuntimeInstance@@3PEAVRuntimeInstance@@EA
EXTERN g_pTheRuntimeInstance : QWORD
RuntimeInstance__ShouldHijackLoopForGcStress equ ?ShouldHijackLoopForGcStress@RuntimeInstance@@QEAA_N_K@Z
EXTERN RuntimeInstance__ShouldHijackLoopForGcStress : PROC

endif ;; FEATURE_GC_STRESS

EXTERN g_fGcStressStarted : DWORD
EXTERN g_fHasFastFxsave : BYTE

FXSAVE_SIZE             equ 512

;; Trap to GC.
;; Set up the P/Invoke transition frame with the return address as the safe point.
;; All registers, both volatile and non-volatile, are preserved.
;; The function should be called not jumped because it's expecting the return address
NESTED_ENTRY RhpTrapToGC, _TEXT

    sizeof_OutgoingScratchSpace equ 20h
    sizeof_PInvokeFrame         equ OFFSETOF__PInvokeTransitionFrame__m_PreservedRegs + 15*8
    sizeof_XmmAlignPad          equ 8
    sizeof_XmmSave              equ FXSAVE_SIZE
    sizeof_MachineFrame         equ 6*8
    sizeof_InitialPushedArgs    equ 2*8             ;; eflags, return value
    sizeof_FixedFrame           equ sizeof_OutgoingScratchSpace + sizeof_PInvokeFrame + sizeof_XmmAlignPad + sizeof_XmmSave + sizeof_MachineFrame

        ;; On the stack on entry:
        ;;   [rsp     ]  -> Return address

        ;; save eflags before we trash them
        pushfq

        ;; What we want to get to:
        ;;
        ;;   [rsp     ]  -> outgoing scratch area
        ;;
        ;;   [rsp + 20]  -> m_RIP                           -------|
        ;;   [rsp + 28]  -> m_FramePointer                         |
        ;;   [rsp + 30]  -> m_pThread                              |
        ;;   [rsp + 38]  -> m_Flags / m_dwAlignPad2                |
        ;;   [rsp + 40]  -> rbx save                               |
        ;;   [rsp + 48]  -> rsi save                               |
        ;;   [rsp + 50]  -> rdi save                               |
        ;;   [rsp + 58]  -> r12 save                               |
        ;;   [rsp + 60]  -> r13 save                               |
        ;;   [rsp + 68]  -> r14 save                               | PInvokeTransitionFrame
        ;;   [rsp + 70]  -> r15 save                               |
        ;;   [rsp + 78]  -> rsp save                               |
        ;;   [rsp + 80]  -> rax save                               |
        ;;   [rsp + 88]  -> rcx save                               |
        ;;   [rsp + 90]  -> rdx save                               |
        ;;   [rsp + 98]  -> r8 save                                |
        ;;   [rsp + a0]  -> r9 save                                |
        ;;   [rsp + a8]  -> r10 save                               |
        ;;   [rsp + b0]  -> r11 save                        -------|
        ;;
        ;;   [rsp + b8]  -> [XmmAlignPad]
        ;;
        ;;   [rsp + c0]  -> FXSAVE area
        ;;
        ;;   [rsp +2c0]  | RIP      |
        ;;   [rsp +2c8]  | CS       |
        ;;   [rsp +2d0]  | EFLAGS   | <-- 'machine frame'
        ;;   [rsp +2d8]  | RSP      |
        ;;   [rsp +2e0]  | SS       |
        ;;   [rsp +2e8]  | padding  |
        ;;
        ;;   [rsp +2f0]  [PSP]
        ;;   [rsp +2f8]  [optional stack alignment]
        ;;
        ;;   [PSP - 10] -> eflags save
        ;;   [PSP -  8] -> Return address
        ;;   [PSP]      -> caller's frame

        test        rsp, 0Fh
        jz          AlreadyAligned

        sub         rsp, sizeof_XmmAlignPad + sizeof_XmmSave + sizeof_MachineFrame + 8 ; +8 to save PSP,
        push        r11                         ; save incoming R11 into save location
        lea         r11, [rsp + 8 + sizeof_XmmAlignPad + sizeof_XmmSave + sizeof_MachineFrame + 8 + sizeof_InitialPushedArgs]
        jmp         PspCalculated

    AlreadyAligned:

        sub         rsp, sizeof_XmmAlignPad + sizeof_XmmSave + sizeof_MachineFrame + 16 ; +8 to save RSP, +8 to re-align PSP,
        push        r11                         ; save incoming R11 into save location
        lea         r11, [rsp + 8 + sizeof_XmmAlignPad + sizeof_XmmSave + sizeof_MachineFrame + 16 + sizeof_InitialPushedArgs]

    PspCalculated:
        push        r10                         ; save incoming R10 into save location
        xor         r10d, r10d

        ;;
        ;; Populate the 'machine frame' in the diagram above.  We have only pushed up to the 'r10 save', so we have not
        ;; yet pushed 0xA8 bytes of that diagram.
        ;;
        ;; [rsp + {offset-in-target-frame-layout-diagram} - {as-yet-unpushed-stack-size}]
        mov         [rsp + 2c0h - 0a8h], r10           ; init RIP to zero
        mov         [rsp + 2c8h - 0a8h], r10           ; init CS to zero
        mov         [rsp + 2d0h - 0a8h], r10           ; init EFLAGS to zero
        mov         [rsp + 2d8h - 0a8h], r11           ; save PSP in the 'machine frame'
        mov         [rsp + 2e0h - 0a8h], r10           ; init SS to zero
        mov         [rsp + 2f0h - 0a8h], r11           ; save PSP

        .pushframe
        .allocstack sizeof_XmmAlignPad + sizeof_XmmSave + 2*8    ;; only 2 of the regs from the PInvokeTransitionFrame are on the stack

        push_vol_reg    r9
        push_vol_reg    r8
        push_vol_reg    rdx
        push_vol_reg    rcx
        push_vol_reg    rax
        push_vol_reg    r11         ; PSP gets saved into the PInvokeTransitionFrame
        push_nonvol_reg r15
        push_nonvol_reg r14
        push_nonvol_reg r13
        push_nonvol_reg r12
        push_nonvol_reg rdi
        push_nonvol_reg rsi
        push_nonvol_reg rbx
        push_vol_reg    PROBE_SAVE_FLAGS_EVERYTHING     ; m_Flags / m_dwAlignPad2

        ;; rdx <- GetThread(), TRASHES rcx
        INLINE_GETTHREAD rdx, rcx

        push_vol_reg    rdx                             ; m_pThread
        push_nonvol_reg rbp                             ; m_FramePointer
        push_vol_reg    r10                             ; m_RIP

        alloc_stack     sizeof_OutgoingScratchSpace
        END_PROLOGUE

        mov         rbx, r11    ; put PSP into RBX
        mov         rsi, rdx    ; put Thread* into RSI

        ; RBX is PSP
        ; RSI is Thread*

        fxsave      [rsp + 0c0h]

        cmp         [g_fHasFastFxsave], 0   ; fast fxsave won't save the xmm registers, so we must do it
        jz          DontSaveXmmAgain

        ;; 0C0h -> offset of FXSAVE area
        ;; 0A0h -> offset of xmm0 save area within the FXSAVE area
        movdqa      [rsp + 0c0h + 0a0h +  0*10h], xmm0
        movdqa      [rsp + 0c0h + 0a0h +  1*10h], xmm1
        movdqa      [rsp + 0c0h + 0a0h +  2*10h], xmm2
        movdqa      [rsp + 0c0h + 0a0h +  3*10h], xmm3
        movdqa      [rsp + 0c0h + 0a0h +  4*10h], xmm4
        movdqa      [rsp + 0c0h + 0a0h +  5*10h], xmm5
        movdqa      [rsp + 0c0h + 0a0h +  6*10h], xmm6
        movdqa      [rsp + 0c0h + 0a0h +  7*10h], xmm7
        movdqa      [rsp + 0c0h + 0a0h +  8*10h], xmm8
        movdqa      [rsp + 0c0h + 0a0h +  9*10h], xmm9
        movdqa      [rsp + 0c0h + 0a0h + 10*10h], xmm10
        movdqa      [rsp + 0c0h + 0a0h + 11*10h], xmm11
        movdqa      [rsp + 0c0h + 0a0h + 12*10h], xmm12
        movdqa      [rsp + 0c0h + 0a0h + 13*10h], xmm13
        movdqa      [rsp + 0c0h + 0a0h + 14*10h], xmm14
        movdqa      [rsp + 0c0h + 0a0h + 15*10h], xmm15

DontSaveXmmAgain:
        mov         rax, [rbx - 8]
        mov         [rsp + 2c0h], rax       ; save return address into 'machine frame'
        mov         [rsp +  20h], rax       ; save return address into PInvokeTransitionFrame

        ; Early out if GC stress is currently suppressed. Do this after we have computed the real address to
        ; return to but before we link the transition frame onto m_pDeferredTransitionFrame (because hitting this
        ; condition implies we're running restricted callouts during a GC itself and we could end up
        ; overwriting a co-op frame set by the code that caused the GC in the first place, e.g. a GC.Collect
        ; call).
        test        dword ptr [rsi + OFFSETOF__Thread__m_ThreadStateFlags], TSF_SuppressGcStress + TSF_DoNotTriggerGc
        jnz         DoneWaitingForGc

        ; link the frame into the Thread
        lea         rcx, [rsp + sizeof_OutgoingScratchSpace]    ; rcx <- PInvokeTransitionFrame*
        mov         [rsi + OFFSETOF__Thread__m_pDeferredTransitionFrame], rcx

        ;;
        ;; Unhijack this thread, if necessary.
        ;;
        INLINE_THREAD_UNHIJACK  rsi, rax, rcx       ;; trashes RAX, RCX

ifdef FEATURE_GC_STRESS
        xor         eax, eax
        cmp         [g_fGcStressStarted], eax
        jz          @F

        mov         rdx, [rsp + 2c0h]
        mov         rcx, [g_pTheRuntimeInstance]
        call        RuntimeInstance__ShouldHijackLoopForGcStress
        cmp         al, 0
        je          @F

        call        REDHAWKGCINTERFACE__STRESSGC
@@:
endif ;; FEATURE_GC_STRESS

        lea         rcx, [rsp + sizeof_OutgoingScratchSpace]    ; calculate PInvokeTransitionFrame pointer
        call        RhpWaitForGCNoAbort

    DoneWaitingForGc:

        fxrstor     [rsp + 0c0h]

        cmp         [g_fHasFastFxsave], 0
        jz          DontRestoreXmmAgain

        movdqa      xmm0 , [rsp + 0c0h + 0a0h +  0*10h]
        movdqa      xmm1 , [rsp + 0c0h + 0a0h +  1*10h]
        movdqa      xmm2 , [rsp + 0c0h + 0a0h +  2*10h]
        movdqa      xmm3 , [rsp + 0c0h + 0a0h +  3*10h]
        movdqa      xmm4 , [rsp + 0c0h + 0a0h +  4*10h]
        movdqa      xmm5 , [rsp + 0c0h + 0a0h +  5*10h]
        movdqa      xmm6 , [rsp + 0c0h + 0a0h +  6*10h]
        movdqa      xmm7 , [rsp + 0c0h + 0a0h +  7*10h]
        movdqa      xmm8 , [rsp + 0c0h + 0a0h +  8*10h]
        movdqa      xmm9 , [rsp + 0c0h + 0a0h +  9*10h]
        movdqa      xmm10, [rsp + 0c0h + 0a0h + 10*10h]
        movdqa      xmm11, [rsp + 0c0h + 0a0h + 11*10h]
        movdqa      xmm12, [rsp + 0c0h + 0a0h + 12*10h]
        movdqa      xmm13, [rsp + 0c0h + 0a0h + 13*10h]
        movdqa      xmm14, [rsp + 0c0h + 0a0h + 14*10h]
        movdqa      xmm15, [rsp + 0c0h + 0a0h + 15*10h]

DontRestoreXmmAgain:
        add         rsp, sizeof_OutgoingScratchSpace
        mov         eax, [rsp + OFFSETOF__PInvokeTransitionFrame__m_Flags]
        test        eax, PTFF_THREAD_ABORT
        pop         rax                     ; m_RIP
        pop         rbp                     ; m_FramePointer
        pop         rax                     ; m_pThread
        pop         rax                     ; m_Flags / m_dwAlign2
        pop         rbx
        pop         rsi
        pop         rdi
        pop         r12
        pop         r13
        pop         r14
        pop         r15
        pop         rax                     ; RSP
        pop         rax                     ; RAX save
        pop         rcx
        pop         rdx
        pop         r8
        pop         r9
        pop         r10
        pop         r11

        ;; restore PSP
        ;; 2F0h -> offset of the PSP area
        ;; 0B8h -> offset of the end of the integer register area which is already popped
        mov         rsp, [rsp + 2f0h - 0b8h]

        ;; RSP is PSP at this point and the stack looks like this:
        ;;   [PSP - 10] -> eflags save
        ;;   [PSP -  8] -> return address
        ;;   [PSP]      -> caller's frame
        ;;
        ;; The final step is to restore eflags and return

        lea         rsp, [rsp - 10h]
        jz          @f          ;; result of the test instruction before the pops above
        popfq                   ;; restore flags
        mov         rcx, STATUS_REDHAWK_THREAD_ABORT
        pop         rdx         ;; return address as exception RIP
        jmp         RhpThrowHwEx ;; Throw the ThreadAbortException as a special kind of hardware exception

@@:
        popfq               ;; restore flags
        ret

NESTED_END RhpTrapToGC, _TEXT

ifdef FEATURE_GC_STRESS
;;
;; INVARIANT: Don't trash the argument registers, the binder codegen depends on this.
;;
LEAF_ENTRY RhpSuppressGcStress, _TEXT

        INLINE_GETTHREAD    rax, r10
   lock or          dword ptr [rax + OFFSETOF__Thread__m_ThreadStateFlags], TSF_SuppressGcStress
        ret

LEAF_END RhpSuppressGcStress, _TEXT
endif ;; FEATURE_GC_STRESS

LEAF_ENTRY RhpGcPoll, _TEXT

        cmp         [RhpTrapThreads], TrapThreadsFlags_None
        jne         @F                  ; forward branch - predicted not taken
        ret
@@:
        jmp         RhpGcPollRare

LEAF_END RhpGcPoll, _TEXT

NESTED_ENTRY RhpGcPollRare, _TEXT

        PUSH_COOP_PINVOKE_FRAME rcx
        END_PROLOGUE

        call        RhpGcPoll2

        POP_COOP_PINVOKE_FRAME

        ret

NESTED_END RhpGcPollRare, _TEXT

        end

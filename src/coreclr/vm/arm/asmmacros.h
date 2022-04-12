// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

;-----------------------------------------------------------------------------
; Macro used to assign an alternate name to a symbol containing characters normally disallowed in a symbol
; name (e.g. C++ decorated names).
    MACRO
      SETALIAS   $name, $symbol
        GBLS    $name
$name   SETS    "|$symbol|"
    MEND


;-----------------------------------------------------------------------------
; Macro used to end a function with explicit _End label
    MACRO
    LEAF_END_MARKED $FuncName

    LCLS __EndLabelName
__EndLabelName SETS "$FuncName":CC:"_End"
    EXPORT $__EndLabelName
$__EndLabelName

    LEAF_END $FuncName

    ; Make sure this symbol gets its own address
    nop

    MEND

;-----------------------------------------------------------------------------
; Macro use for enabling C++ to know where to patch code at runtime.
    MACRO
    PATCH_LABEL $FuncName
$FuncName
    EXPORT $FuncName

    MEND

;-----------------------------------------------------------------------------
; Macro used to check (in debug builds only) whether the stack is 64-bit aligned (a requirement before calling
; out into C++/OS code). Invoke this directly after your prolog (if the stack frame size is fixed) or directly
; before a call (if you have a frame pointer and a dynamic stack). A breakpoint will be invoked if the stack
; is misaligned.
;
    MACRO
        CHECK_STACK_ALIGNMENT

#ifdef _DEBUG
        push    {r0}
        add     r0, sp, #4
        tst     r0, #7
        pop     {r0}
        beq     %F0
        EMIT_BREAKPOINT
0
#endif
    MEND

;-----------------------------------------------------------------------------
; The following group of macros assist in implementing prologs and epilogs for methods that set up some
; subclass of TransitionFrame. They ensure that the SP is 64-bit aligned at the conclusion of the prolog and
; provide a helper macro to locate the start of the NegInfo (if there is one) for the frame.

;-----------------------------------------------------------------------------
; Define the prolog for a TransitionFrame-based method. This macro should be called first in the method and
; comprises the entire prolog (i.e. don't modify SP after calling this). Takes the size of the frame's NegInfo
; (which may be zero) and the frame itself. No initialization of the frame is done beyond callee saved
; registers and (non-floating point) argument registers.
;
    MACRO
        PROLOG_WITH_TRANSITION_BLOCK $extraLocals, $SaveFPArgs, $PushArgRegs

        GBLA __PWTB_FloatArgumentRegisters
        GBLA __PWTB_StackAlloc
        GBLA __PWTB_TransitionBlock
        GBLL __PWTB_SaveFPArgs

        IF "$SaveFPArgs" != ""
__PWTB_SaveFPArgs SETL $SaveFPArgs
        ELSE
__PWTB_SaveFPArgs SETL {true}
        ENDIF

        IF "$extraLocals" != ""
__PWTB_FloatArgumentRegisters SETA $extraLocals
        ELSE
__PWTB_FloatArgumentRegisters SETA 0
        ENDIF

        IF __PWTB_SaveFPArgs

        IF __PWTB_FloatArgumentRegisters:MOD:8 != 0
__PWTB_FloatArgumentRegisters SETA __PWTB_FloatArgumentRegisters + 4
        ENDIF
__PWTB_TransitionBlock SETA __PWTB_FloatArgumentRegisters + (SIZEOF__FloatArgumentRegisters + 4) ; padding

        ELSE

        IF __PWTB_FloatArgumentRegisters:MOD:8 == 0
__PWTB_FloatArgumentRegisters SETA __PWTB_FloatArgumentRegisters + 4; padding
        ENDIF
__PWTB_TransitionBlock SETA __PWTB_FloatArgumentRegisters

        ENDIF

__PWTB_StackAlloc SETA __PWTB_TransitionBlock

        IF "$PushArgRegs" != "DoNotPushArgRegs"
        ; Spill argument registers.
        PROLOG_PUSH         {r0-r3}
        ENDIF

        ; Spill callee saved registers and return address.
        PROLOG_PUSH         {r4-r11,lr}

        ; Allocate space for the rest of the frame
        PROLOG_STACK_ALLOC  __PWTB_StackAlloc

        IF __PWTB_SaveFPArgs
        add         r6, sp, #(__PWTB_FloatArgumentRegisters)
        vstm        r6, {s0-s15}
        ENDIF

        CHECK_STACK_ALIGNMENT
    MEND

;-----------------------------------------------------------------------------
; Provides a matching epilog to PROLOG_WITH_TRANSITION_BLOCK and ends by preparing for tail-calling.
; Since this is a tail call argument registers are restored.
;
    MACRO
        EPILOG_WITH_TRANSITION_BLOCK_TAILCALL

        IF __PWTB_SaveFPArgs
        add         r6, sp, #(__PWTB_FloatArgumentRegisters)
        vldm        r6, {s0-s15}
        ENDIF

        EPILOG_STACK_FREE   __PWTB_StackAlloc
        EPILOG_POP          {r4-r11,lr}
        EPILOG_POP          {r0-r3}
    MEND

;-----------------------------------------------------------------------------
; Provides a matching epilog to PROLOG_WITH_TRANSITION_FRAME and ends by returning to the original caller.
; Since this is not a tail call argument registers are not restored.
;
    MACRO
        EPILOG_WITH_TRANSITION_BLOCK_RETURN

        EPILOG_STACK_FREE   __PWTB_StackAlloc
        EPILOG_POP          {r4-r11,lr}
        EPILOG_STACK_FREE   16
        EPILOG_RETURN
    MEND

;-----------------------------------------------------------------------------
; Macro to get a pointer to the Thread* object for the currently executing thread
;
__tls_array     equ 0x2C    ;; offsetof(TEB, ThreadLocalStoragePointer)

        GBLS __SECTIONREL_gCurrentThreadInfo
__SECTIONREL_gCurrentThreadInfo SETS "SECTIONREL_gCurrentThreadInfo"

    MACRO
        INLINE_GETTHREAD $destReg, $trashReg
        EXTERN _tls_index

        ldr         $destReg, =_tls_index
        ldr         $destReg, [$destReg]
        mrc         p15, 0, $trashReg, c13, c0, 2
        ldr         $trashReg, [$trashReg, #__tls_array]
        ldr         $destReg, [$trashReg, $destReg, lsl #2]
        ldr         $trashReg, $__SECTIONREL_gCurrentThreadInfo
        ldr         $destReg,[$destReg, $trashReg]     ; return gCurrentThreadInfo.m_pThread
    MEND

;-----------------------------------------------------------------------------
; INLINE_GETTHREAD_CONSTANT_POOL macro has to be used after the last function in the .asm file that used
; INLINE_GETTHREAD. Optionally, it can be also used after any function that used INLINE_GETTHREAD
; to improve density, or to reduce distance betweeen the constant pool and its use.
;
    SETALIAS gCurrentThreadInfo, ?gCurrentThreadInfo@@3UThreadLocalInfo@@A

    MACRO
        INLINE_GETTHREAD_CONSTANT_POOL
        EXTERN $gCurrentThreadInfo

$__SECTIONREL_gCurrentThreadInfo
        DCDU $gCurrentThreadInfo
        RELOC 15 ;; SECREL

__SECTIONREL_gCurrentThreadInfo SETS "$__SECTIONREL_gCurrentThreadInfo":CC:"_"
    MEND

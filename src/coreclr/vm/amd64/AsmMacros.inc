; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include amd64\AsmMacros_Windows.inc

;
; Inlined version of GetThread
; Trashes rax and r11
;
t_CurrentThreadInfo  TEXTEQU <?t_CurrentThreadInfo@@3UThreadLocalInfo@@A>

INLINE_GETTHREAD macro Reg

        EXTERN _tls_index : DWORD
        EXTERN t_CurrentThreadInfo:DWORD

        mov     r11d, [_tls_index]
        mov     rax, gs:[OFFSET__TEB__ThreadLocalStoragePointer]
        mov     rax, [rax + r11 * 8]
        mov     r11d, SECTIONREL t_CurrentThreadInfo
        mov     Reg, [rax + r11]

        endm

; if you change this code there will be corresponding code in JITInterfaceGen.cpp which will need to be changed
;

JIT_HELPER_MONITOR_THUNK macro THUNK_NAME, Section
Section segment para 'CODE'
        align 16
        public THUNK_NAME
THUNK_NAME proc
        xor     edx, edx
THUNK_NAME endp
Section ends
        endm


t_runtime_thread_locals  TEXTEQU <?t_runtime_thread_locals@@3URuntimeThreadLocals@@A>

; Inlined version of GetThreadEEAllocContext. Trashes RegTrash, r11
INLINE_GET_ALLOC_CONTEXT_BASE macro Reg, RegTrash

        EXTERN _tls_index : DWORD
        EXTERN t_runtime_thread_locals : DWORD

        mov     r11d, [_tls_index]
        mov     RegTrash, gs:[OFFSET__TEB__ThreadLocalStoragePointer]
        mov     RegTrash, [RegTrash + r11 * 8]
        mov     r11d, SECTIONREL t_runtime_thread_locals
        lea     Reg, [RegTrash + r11]

        endm

OFFSETOF__ee_alloc_context  EQU OFFSETOF__RuntimeThreadLocals__ee_alloc_context

; Pushes a TransitionBlock on the stack without saving the argument registers. See
; the PROLOG_WITH_TRANSITION_BLOCK macro for the stack layout.
PUSH_COOP_PINVOKE_FRAME macro target

        __PWTB_StackAlloc = SIZEOF_MAX_OUTGOING_ARGUMENT_HOMES + 8 ; alignment to make the stack 16b aligned
        __PWTB_TransitionBlock = __PWTB_StackAlloc

        PUSH_CALLEE_SAVED_REGISTERS
        alloc_stack     __PWTB_StackAlloc
        END_PROLOGUE
        lea     target, [rsp + __PWTB_TransitionBlock]

        endm

POP_COOP_PINVOKE_FRAME macro

        add rsp, __PWTB_StackAlloc
        POP_CALLEE_SAVED_REGISTERS

        endm

; Pushes a full TransitionBlock on the stack including argument registers and
; floating point argument registers. Used for exception throw helpers where we
; need to capture the complete register state including FP callee-saved registers.
;
; Stack layout (from high to low address after prologue):
;   Outgoing argument homes (32 bytes)
;   Return address (m_ReturnAddress)
;   CalleeSavedRegisters (r15, r14, r13, r12, rbp, rbx, rsi, rdi - 64 bytes) <- TransitionBlock starts here
;   FloatArgumentRegisters (xmm0-xmm3, 64 bytes)
;   FP Callee-saved registers (xmm6-xmm15, 160 bytes)
;   Shadow space for call (32 bytes)
;   sp points here
;
; On exit, target contains the TransitionBlock pointer (CalleeSavedRegisters).
PUSH_COOP_PINVOKE_FRAME_WITH_FLOATS macro target

        PUSH_CALLEE_SAVED_REGISTERS

        ; Allocate space for: shadow for call (32) + FP callee-saved (160) + float args (64) + arg regs (32) + padding (8) = 296 bytes
        ; Shadow space at offset 0 is reserved for the call to IL_Throw_Impl etc.
        ; This makes RSP 16-byte aligned (8 + 64 + 296 = 368, and original RSP - 368 is 16-byte aligned)
        alloc_stack     296

        ; Save argument registers at offset 256 (32 + 160 + 64)
        SAVE_ARGUMENT_REGISTERS 256

        ; Save float argument registers at offset 192 (32 + 160)
        SAVE_FLOAT_ARGUMENT_REGISTERS 192

        ; Save FP callee-saved registers (xmm6-xmm15) at offset 32 (after shadow space)
        ; RSP is 16-byte aligned, so offset 32, 48, 64, ... are all 16-byte aligned
        ; AND these offsets are multiples of 16 as required by unwind codes
        save_xmm128_postrsp xmm6,  20h
        save_xmm128_postrsp xmm7,  30h
        save_xmm128_postrsp xmm8,  40h
        save_xmm128_postrsp xmm9,  50h
        save_xmm128_postrsp xmm10, 60h
        save_xmm128_postrsp xmm11, 70h
        save_xmm128_postrsp xmm12, 80h
        save_xmm128_postrsp xmm13, 90h
        save_xmm128_postrsp xmm14, 0A0h
        save_xmm128_postrsp xmm15, 0B0h

        END_PROLOGUE

        ; TransitionBlock pointer points to CalleeSavedRegisters at rsp + 296
        lea     target, [rsp + 296]

        endm

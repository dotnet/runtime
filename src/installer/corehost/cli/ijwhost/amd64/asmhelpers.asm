; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

include AsmMacros.inc

extern  start_runtime_and_get_target_address:proc

; Stack setup at time of call to start_runtime_and_get_target_address
;   32-byte scratch space
;   xmm0 (saved incoming arg)
;   xmm1 (saved incoming arg)
;   xmm2 (saved incoming arg)
;   xmm3 (saved incoming arg)
;   8-byte padding
;   return address
;   rcx (saved incoming arg)    <- 16-byte aligned scratch space of caller
;   rdx (saved incoming arg)
;   r8  (saved incoming arg)
;   r9  (saved incoming arg)

SIZEOF_SCRATCH_SPACE                equ 20h
SIZEOF_FP_ARG_SPILL                 equ 10h*4   ; == 40h
SIZEOF_PADDING                      equ 8h

SIZEOF_ALLOC_STACK                  equ SIZEOF_SCRATCH_SPACE + SIZEOF_FP_ARG_SPILL + SIZEOF_PADDING

SIZEOF_RET_ADDR                     equ 8h

; rcx, rdx, r8, r9 need preserving, in the scratch area
SIZEOF_INCOMING_ARG_SPILL           equ 8h*4    ; == 20h

; xmm0 - xmm3 need preserving.
OFFSETOF_SCRATCH_SPACE              equ 0h
OFFSETOF_FP_ARG_SPILL               equ OFFSETOF_SCRATCH_SPACE + SIZEOF_SCRATCH_SPACE
OFFSETOF_PADDING                    equ OFFSETOF_FP_ARG_SPILL + SIZEOF_FP_ARG_SPILL
OFFSETOF_RET_ADDR                   equ OFFSETOF_PADDING + SIZEOF_PADDING
OFFSETOF_INCOMING_ARG_SPILL         equ OFFSETOF_RET_ADDR + SIZEOF_RET_ADDR

NESTED_ENTRY start_runtime_thunk_stub, _TEXT
    ; Allocate the stack space
    alloc_stack     SIZEOF_ALLOC_STACK

    ; Save the incoming floating point arguments
    save_xmm128     xmm0,    0h + OFFSETOF_FP_ARG_SPILL
    save_xmm128     xmm1,   10h + OFFSETOF_FP_ARG_SPILL
    save_xmm128     xmm2,   20h + OFFSETOF_FP_ARG_SPILL
    save_xmm128     xmm3,   30h + OFFSETOF_FP_ARG_SPILL

    ; Save the incoming arguments into the scratch area
    save_reg        rcx,     0h + OFFSETOF_INCOMING_ARG_SPILL
    save_reg        rdx,     8h + OFFSETOF_INCOMING_ARG_SPILL
    save_reg        r8,     10h + OFFSETOF_INCOMING_ARG_SPILL
    save_reg        r9,     18h + OFFSETOF_INCOMING_ARG_SPILL

    END_PROLOGUE

    ; Secret arg is in r10.
    mov             rcx,    r10

    ; Call helper func.
    call            start_runtime_and_get_target_address

    ; Restore the incoming floating point arguments
    movdqa          xmm0,   [rsp +  0h + OFFSETOF_FP_ARG_SPILL]
    movdqa          xmm1,   [rsp + 10h + OFFSETOF_FP_ARG_SPILL]
    movdqa          xmm2,   [rsp + 20h + OFFSETOF_FP_ARG_SPILL]
    movdqa          xmm3,   [rsp + 30h + OFFSETOF_FP_ARG_SPILL]

    ; Restore the incoming arguments
    mov             rcx,    [rsp +  0h + OFFSETOF_INCOMING_ARG_SPILL]
    mov             rdx,    [rsp +  8h + OFFSETOF_INCOMING_ARG_SPILL]
    mov             r8,     [rsp + 10h + OFFSETOF_INCOMING_ARG_SPILL]
    mov             r9,     [rsp + 18h + OFFSETOF_INCOMING_ARG_SPILL]

    ; Restore the stack
    add             rsp,    SIZEOF_ALLOC_STACK

    ; Jump to the target
    TAILJMP_RAX
NESTED_END start_runtime_thunk_stub, _TEXT

;LEAF_ENTRY start_runtime_thunk_stubSample, _TEXT
;    mov             r10,    1234567812345678h
;    mov             r11,    1234123412341234h
;    jmp             r11
;LEAF_END start_runtime_thunk_stubSample, _TEXT

    end


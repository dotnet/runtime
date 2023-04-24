; Licensed to the .NET Foundation under one or more agreements.
; The .NET Foundation licenses this file to you under the MIT license.

; ***********************************************************************
; File: CrtHelpers.asm
;
; ***********************************************************************

#include "ksarm.h"

#include "asmconstants.h"

#include "asmmacros.h"

    TEXTAREA

; JIT_MemSet/JIT_MemCpy
;
; It is IMPORANT that the exception handling code is able to find these guys
; on the stack, but to keep them from being tailcalled by VC++ we need to turn
; off optimization and it ends up being a wasteful implementation.
;
; Hence these assembly helpers.
;
;EXTERN_C void __stdcall JIT_MemSet(void* _dest, int c, size_t count)
        LEAF_ENTRY JIT_MemSet

;
;       The memset function sets the first count bytes of
;       dest to the character c (r1).
;
; Doesn't return a value
;

        subs    r2, r2, #4
        blt     ByteSet

        ands    r1, r1, #&FF
        orr     r1, r1, r1, lsl #8
CheckAlign                                              ; 2-3 cycles
        ands    r3, r0, #3                              ; Check alignment and fix if possible
        bne     Align

BlockSet                                                ; 6-7 cycles
        orr     r1, r1, r1, lsl #16
        subs    r2, r2, #12
        mov     r3, r1
        blt     BlkSet8

BlkSet16                                                ; 7 cycles/16 bytes
        stm     r0!, {r1, r3}
        subs    r2, r2, #16
        stm     r0!, {r1, r3}
        bge     BlkSet16

BlkSet8                                                 ; 4 cycles/8 bytes
        adds    r2, r2, #8
        blt     BlkSet4
        stm     r0!, {r1, r3}
        sub     r2, r2, #8

BlkSet4
        adds    r2, r2, #4                              ; 4 cycles/4 bytes
        blt     ByteSet
        str     r1, [r0], #4
        b       MaybeExit

ByteSet
        adds    r2, r2, #4
MaybeExit
        beq     ExitMemSet

        strb    r1, [r0]                                ; 5 cycles/1-3bytes
        cmp     r2, #2
        blt     ExitMemSet
        strb    r1, [r0, #1]
        strbgt  r1, [r0, #2]

ExitMemSet

        bx      lr

Align                                                   ; 8 cycles/1-3 bytes
        tst     r0, #1                                  ; Check byte alignment
        beq     AlignHalf
        subs    r2, r2, #1
        strb    r1, [r0], #1
AlignHalf
        tst     r0, #2                                  ; Check Half-word alignment
        beq     BlockSet
        subs    r2, r2, #2
        strh    r1, [r0], #2
        b       BlockSet

        LEAF_END_MARKED JIT_MemSet


;EXTERN_C void __stdcall JIT_MemCpy(void* _dest, const void *_src, size_t count)
        LEAF_ENTRY JIT_MemCpy
;
; It only requires 4 byte alignment
; and doesn't return a value

        cmp     r2, #0                                  ; quick check for 0 length
        beq     ExitMemCpy                              ; if zero, exit

        tst     r0, #3                                  ; skip directly to aligned if already aligned
        beq     DestAligned                             ; if 0, we're already aligned; go large

ByteLoop1
        subs    r2, r2, #1                              ; decrement byte counter
        ldrb    r3, [r1], #1                            ; copy one byte
        strb    r3, [r0], #1
        beq     ExitMemCpy                              ; if the byte counter hits 0, exit early
        tst     r0, #3                                  ; are we aligned now?
        bne     ByteLoop1                               ; nope, keep going

DestAligned
        subs    r2, r2, #8                              ; byte counter -= 8
        blt     AlignedFinished                         ; if that puts us negative, skip the big copy

        tst     r1, #3                                  ; is the 4-byte source aligned?
        addne   r2, r2, #8                              ; if not, fix the byte counter (+= 8)
        bne     ByteLoop2                               ; and do all the rest with bytes

QwordLoop
        subs    r2, r2, #8                              ; decrement byte counter by 8
        ldm     r1!, {r3,r12}                           ; copy one qword
        stm     r0!, {r3,r12}                           ;
        bge     QwordLoop                               ; loop until the byte counter goes negative

AlignedFinished
        adds    r2, r2, #4				; add 4 to recover a potential >= 4-byte tail
        blt     AlignedFinished2
        ldr     r3, [r1], #4
        str     r3, [r0], #4
        b       MaybeExitMemCpy
AlignedFinished2
        adds    r2, r2, #4                              ; add 4 more to the byte counter to recover

MaybeExitMemCpy
        beq     ExitMemCpy                              ; the remaining count

ByteLoop2
        subs    r2, r2, #1                              ; decrement the counter
        ldrb    r3, [r1], #1                            ; copy one byte
        strb    r3, [r0], #1
        bne     ByteLoop2                               ; loop until the counter hits 0

ExitMemCpy
        bx      lr

        LEAF_END_MARKED JIT_MemCpy

        END

